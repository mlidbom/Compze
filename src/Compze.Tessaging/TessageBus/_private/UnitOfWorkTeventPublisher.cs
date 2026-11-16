using Compze.Tessaging.Validation._internal;
using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageBus._private.BestEffortDelivery;
using Compze.Tessaging.TessageBus._private.Outbox;
using Compze.Tessaging.TessageBus._private.TeventDispatch;
using Compze.Tessaging.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.TessageBus._private;

static class UnitOfWorkTeventPublisherRegistrar
{
   public static IComponentRegistrar UnitOfWorkTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(TransactionTeventQueues.RegisterWith, _private.UnitOfWorkTeventPublisher.RegisterWith);
}

///<summary>The <see cref="IUnitOfWorkTeventPublisher"/>: checks each published tevent at once, and queues it in the<br/>
/// <see cref="TransactionTeventQueue"/> of the caller's transaction, which dispatches it when the transaction prepares to commit,<br/>
/// routed by the delivery contract its type declares.<br/>
/// Participation — synchronous delivery to this process's handlers through the engine's one executor<br/>
/// (<see cref="TessageHandlerExecutor"/>), within the caller's transaction — is the leg every tevent travels; an<br/>
/// <see cref="IExactlyOnceTevent"/> additionally travels the endpoint's<br/>
/// <see cref="IExactlyOnceTeventDeliveryLeg"/>, and a remotable tevent whose type declares no exactly-once guarantee the endpoint's<br/>
/// <see cref="IBestEffortTeventDeliveryLeg"/>, when the composition wires them. Zero wired legs is the deliberately in-process<br/>
/// composition, where participation already serves every subscriber that can exist; a remote-capable endpoint missing the leg a<br/>
/// tevent's contract demands is a loud publish error — never a silent downgrade.</summary>
///<remarks>What can be checked at the publish is checked there, so that a wrong publish fails in the code that published: the<br/>
/// ambient transaction, the engine not stopped, the tevent valid to execute here, and the delivery leg its contract demands.</remarks>
[UsedImplicitly] class UnitOfWorkTeventPublisher : IUnitOfWorkTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkTeventPublisher>()
                                  .CreatedBy((TransactionTeventQueues queues, TeventObservationDispatcher observationDispatcher, IComponentSet<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IComponentSet<IBestEffortTeventDeliveryLeg> bestEffortDeliveryLegs, IScopeResolver scopeResolver)
                                                => new UnitOfWorkTeventPublisher(queues, observationDispatcher, exactlyOnceDeliveryLegs, bestEffortDeliveryLegs, scopeResolver)));

   readonly TransactionTeventQueues _queues;
   readonly TeventObservationDispatcher _observationDispatcher;
   readonly IExactlyOnceTeventDeliveryLeg? _exactlyOnceDeliveryLeg;
   readonly IBestEffortTeventDeliveryLeg? _bestEffortDeliveryLeg;
   readonly IScopeResolver _scopeResolver;

   UnitOfWorkTeventPublisher(TransactionTeventQueues queues, TeventObservationDispatcher observationDispatcher, IEnumerable<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IEnumerable<IBestEffortTeventDeliveryLeg> bestEffortDeliveryLegs, IScopeResolver scopeResolver)
   {
      _queues = queues;
      _observationDispatcher = observationDispatcher;
      _exactlyOnceDeliveryLeg = exactlyOnceDeliveryLegs.SingleOrDefault();
      _bestEffortDeliveryLeg = bestEffortDeliveryLegs.SingleOrDefault();
      _scopeResolver = scopeResolver;
   }

   public void Publish(ITevent tevent)
   {
      var wrappedTevent = PublisherTevent.Wrapped(tevent);
      //Synchrony follows the type: an exactly-once publish writes durable rows inside the caller's transaction - database I/O,
      //async end to end by the type's contract - so the synchronous Publish refuses it. (C# cannot exclude a subtype statically,
      //so the type contract is enforced here, exactly as the handler declaration verbs enforce theirs.)
      State.Assert(wrappedTevent is not IPublisherTevent<IExactlyOnceTevent>,
                   () => $"{tevent.GetType().FullName} declares the exactly-once contract, and exactly-once kinds are async end to end: publishing one writes durable rows inside the caller's transaction — database I/O. Publish it through {nameof(IUnitOfWorkTeventPublisher.PublishAsync)}.");
      Enqueue(wrappedTevent);
   }

   //The tevent is only queued here: what is async, the handlers and the outbox's write, runs when the transaction prepares.
   public Task PublishAsync(ITevent tevent)
   {
      Enqueue(PublisherTevent.Wrapped(tevent));
      return Task.CompletedTask;
   }

   //Every tevent is wrapped before routing: a tevent published without a publisher-identifying wrapper is wrapped by the publish methods above, and routing operates on the wrapper's type.
   void Enqueue(IPublisherTevent<ITevent> wrappedTevent)
   {
      State.Assert(Transaction.Current != null,
                   () => $"{nameof(IUnitOfWorkTeventPublisher)} publishes within the caller's unit of work, and there is no ambient transaction — no unit of work to publish within. Run the caller through ExecuteUnitOfWork, or publish through {nameof(IIndependentTeventPublisher)}, which runs each publish as its own unit of work.");
      State.Assert(!_observationDispatcher.HasStopped,
                   () => $"The LocalTessagingEngine has stopped ({nameof(ILocalTessagingEngine)}.{nameof(ILocalTessagingEngine.StopAsync)}), so it publishes no more tevents: no observer would observe {wrappedTevent.Tevent.GetType().FullName}. Publish before stopping the engine.");
      var unitOfWork = UnitOfWorkResolver.From(_scopeResolver);
      var remoteDelivery = RemoteDeliveryFor(wrappedTevent);
      TessageInspector.AssertValidToExecuteLocally(wrappedTevent);
      var queue = _queues.OfTheAmbientTransaction();
      //Observation observes committed facts only: the tevent is queued for its observers when this unit of work commits - a
      //rolled-back publish is never observed - and dispatched off-thread. The hook registers at the publish, so a tevent that a
      //participation handler publishes in response, when the transaction prepares, queues after this one: observers see cause
      //before consequence.
      unitOfWork.OnCommittedSuccessfully(() => _observationDispatcher.QueueForObservers(wrappedTevent));
      queue.Enqueue(new QueuedTevent(wrappedTevent, remoteDelivery));
   }

   ///<summary>The remote delivery the tevent's declared contract selects — resolved and validated at the publish, so an invalid or<br/>
   /// unroutable publish fails in the code that published, before any handler runs. Null when the tevent travels by participation alone:<br/>
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
