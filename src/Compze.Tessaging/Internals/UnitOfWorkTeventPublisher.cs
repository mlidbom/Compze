using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageBus;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Abstractions.Validation;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Internals.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Internals;

static class UnitOfWorkTeventPublisherRegistrar
{
   public static IComponentRegistrar UnitOfWorkTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Internals.UnitOfWorkTeventPublisher.RegisterWith);
}

///<summary>The <see cref="IUnitOfWorkTeventPublisher"/>: routes each published tevent by the delivery contract its type declares.<br/>
/// Participation — synchronous delivery to this process's handlers through the engine's one executor<br/>
/// (<see cref="TessageHandlerExecutor"/>), within the caller's transaction — is the leg every tevent travels; an<br/>
/// <see cref="IExactlyOnceTevent"/> additionally travels the endpoint's<br/>
/// <see cref="IExactlyOnceTeventDeliveryLeg"/>, and a remotable tevent whose type declares no exactly-once guarantee the endpoint's<br/>
/// <see cref="IBestEffortTeventDeliveryLeg"/>, when the composition wires them. Zero wired legs is the deliberately in-process<br/>
/// composition, where participation already serves every subscriber that can exist; a remote-capable endpoint missing the leg a<br/>
/// tevent's contract demands is a loud publish error — never a silent downgrade.</summary>
[UsedImplicitly] class UnitOfWorkTeventPublisher : IUnitOfWorkTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkTeventPublisher>()
                                  .CreatedBy((TessageHandlerExecutor executor, TeventObservationDispatcher observationDispatcher, IComponentSet<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IComponentSet<IBestEffortTeventDeliveryLeg> bestEffortDeliveryLegs, IScopeResolver scopeResolver)
                                                => new UnitOfWorkTeventPublisher(executor, observationDispatcher, exactlyOnceDeliveryLegs, bestEffortDeliveryLegs, scopeResolver)));

   readonly TessageHandlerExecutor _executor;
   readonly TeventObservationDispatcher _observationDispatcher;
   readonly IExactlyOnceTeventDeliveryLeg? _exactlyOnceDeliveryLeg;
   readonly IBestEffortTeventDeliveryLeg? _bestEffortDeliveryLeg;
   readonly IScopeResolver _scopeResolver;

   UnitOfWorkTeventPublisher(TessageHandlerExecutor executor, TeventObservationDispatcher observationDispatcher, IEnumerable<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IEnumerable<IBestEffortTeventDeliveryLeg> bestEffortDeliveryLegs, IScopeResolver scopeResolver)
   {
      _executor = executor;
      _observationDispatcher = observationDispatcher;
      _exactlyOnceDeliveryLeg = exactlyOnceDeliveryLegs.SingleOrDefault();
      _bestEffortDeliveryLeg = bestEffortDeliveryLegs.SingleOrDefault();
      _scopeResolver = scopeResolver;
   }

   public void Publish(ITevent tevent)
   {
      var wrappedTevent = PublisherTevent.Wrapped(tevent);
      //Synchrony follows the type: an exactly-once publish writes durable rows inside the caller's transaction - database I/O,
      //async end to end by the type's contract - so the synchronous door refuses it. (C# cannot exclude a subtype statically,
      //so the type contract is enforced here, exactly as the handler declaration verbs enforce theirs.) With the exactly-once
      //kind excluded, the bridge below bridges only participation - the handlers of the kinds whose contract keeps sync first-class.
      State.Assert(wrappedTevent is not IPublisherTevent<IExactlyOnceTevent>,
                   () => $"{tevent.GetType().FullName} declares the exactly-once contract, and exactly-once kinds are async end to end: publishing one writes durable rows inside the caller's transaction — database I/O. Publish it through {nameof(IUnitOfWorkTeventPublisher.PublishAsync)}.");
      PublishCoreAsync(wrappedTevent).GetAwaiter().GetResult();
   }

   public async Task PublishAsync(ITevent tevent) => await PublishCoreAsync(PublisherTevent.Wrapped(tevent)).caf();

   //Every tevent is wrapped before routing: a tevent published without a publisher-identifying wrapper is wrapped by the doors above, and routing operates on the wrapper's type.
   async Task PublishCoreAsync(IPublisherTevent<ITevent> wrappedTevent)
   {
      State.Assert(Transaction.Current != null,
                   () => $"{nameof(IUnitOfWorkTeventPublisher)} publishes within the caller's unit of work, and there is no ambient transaction — no unit of work to publish within. Run the caller through ExecuteUnitOfWork, or publish through {nameof(IIndependentTeventPublisher)}, which runs each publish as its own unit of work.");
      var unitOfWork = UnitOfWorkResolver.From(_scopeResolver);
      var remoteDelivery = RemoteDeliveryFor(wrappedTevent);
      TessageInspector.AssertValidToExecuteLocally(wrappedTevent);
      //Observation observes committed facts only: the tevent is queued for its observers when this unit of work commits - a
      //rolled-back publish is never observed - and dispatched off-thread. The hook registers before participation runs, so a
      //tevent a participation handler publishes in response queues after this one: observers see cause before consequence.
      unitOfWork.OnCommittedSuccessfully(() => _observationDispatcher.QueueForObservers(wrappedTevent));
      await _executor.ExecuteTeventHandlers(wrappedTevent, unitOfWork).caf();
      if(remoteDelivery != null) await remoteDelivery().caf();
   }

   ///<summary>The remote delivery the tevent's declared contract selects — resolved and validated before participation dispatches,<br/>
   /// so an invalid or unroutable publish fails before any handler runs. Null when the tevent travels by participation alone:<br/>
   /// its type declares no remotability, or the endpoint wires no remote delivery at all.</summary>
   Func<Task>? RemoteDeliveryFor(IPublisherTevent<ITevent> wrappedTevent)
   {
      switch(wrappedTevent)
      {
         case IPublisherTevent<IExactlyOnceTevent> exactlyOnceTevent:
            if(_exactlyOnceDeliveryLeg is not {} exactlyOnceDeliveryLeg)
               return NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(exactlyOnceTevent.Tevent, unwiredLeg: "the exactly-once delivery leg (the outbox, wired by exactly-once Tessaging on a database-backed foundation)");
            TessageInspector.AssertValidToSendRemote(exactlyOnceTevent.Tevent);
            return async () => await exactlyOnceDeliveryLeg.PublishTransactionallyAsync(exactlyOnceTevent).caf();
         case IPublisherTevent<IRemotableTevent> bestEffortTevent:
            if(_bestEffortDeliveryLeg is not {} bestEffortDeliveryLeg)
               return NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(bestEffortTevent.Tevent, unwiredLeg: "the best-effort delivery leg (wired by distributed and exactly-once Tessaging alike)");
            TessageInspector.AssertValidToSendRemote(bestEffortTevent.Tevent);
            return () =>
            {
               bestEffortDeliveryLeg.PublishBestEffort(bestEffortTevent);
               return Task.CompletedTask;
            };
         default:
            return null; //The tevent's type declares no remotability: participation is all the delivery there is.
      }
   }

   ///<summary>Zero wired legs is the deliberately in-process composition — no subscriber outside this process can exist, so<br/>
   /// participation already delivers the tevent's full guarantee and the missing leg is vacuous. On an endpoint that wires ANY<br/>
   /// remote delivery, subscribers may be remote, so a tevent whose contract demands a leg the endpoint did not wire is a loud<br/>
   /// publish error: silently degrading a delivery guarantee is data loss dressed as success.</summary>
   Func<Task>? NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(ITevent tevent, string unwiredLeg)
   {
      State.Assert(_exactlyOnceDeliveryLeg == null && _bestEffortDeliveryLeg == null,
                   () => $"Publishing {tevent.GetType().FullName} demands {unwiredLeg}, which this endpoint does not wire — even though it wires other remote tevent delivery, so its subscribers may be remote. A tevent whose contract needs a delivery leg the endpoint did not wire is a publish error, never a silent downgrade: wire the missing leg, or publish a tevent whose type does not demand it.");
      return null;
   }
}
