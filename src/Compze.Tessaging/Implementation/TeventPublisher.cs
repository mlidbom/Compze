using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Teventive.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class TeventPublisherRegistrar
{
   public static IComponentRegistrar TeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TeventPublisher.RegisterWith);
}

///<summary>The <see cref="ITeventPublisher"/>: routes each published tevent by the delivery contract its type declares.<br/>
/// Participation — synchronous delivery to this process's handlers via <see cref="IInProcessTeventPublisher"/>, within the<br/>
/// caller's transaction — is the leg every tevent travels; an <see cref="IExactlyOnceTevent"/> additionally travels the endpoint's<br/>
/// <see cref="IExactlyOnceTeventDeliveryLeg"/>, and a remotable tevent whose type declares no exactly-once guarantee the endpoint's<br/>
/// <see cref="ITransientTeventDeliveryLeg"/>, when the composition wires them. Zero wired legs is the deliberately in-process<br/>
/// composition, where participation already serves every subscriber that can exist; a remote-capable endpoint missing the leg a<br/>
/// tevent's contract demands is a loud publish error — never a silent downgrade.</summary>
[UsedImplicitly] class TeventPublisher : ITeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ITeventPublisher>()
                                  .CreatedBy((IInProcessTeventPublisher inProcessTeventPublisher, TeventObservationDispatcher teventObservationDispatcher, IComponentSet<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IComponentSet<ITransientTeventDeliveryLeg> transientDeliveryLegs, IScopeResolver scopeResolver)
                                                => new TeventPublisher(inProcessTeventPublisher, teventObservationDispatcher, exactlyOnceDeliveryLegs, transientDeliveryLegs, scopeResolver)));

   readonly IInProcessTeventPublisher _inProcessTeventPublisher;
   readonly TeventObservationDispatcher _teventObservationDispatcher;
   readonly IExactlyOnceTeventDeliveryLeg? _exactlyOnceDeliveryLeg;
   readonly ITransientTeventDeliveryLeg? _transientDeliveryLeg;
   readonly IScopeResolver _scopeResolver;

   TeventPublisher(IInProcessTeventPublisher inProcessTeventPublisher, TeventObservationDispatcher teventObservationDispatcher, IEnumerable<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IEnumerable<ITransientTeventDeliveryLeg> transientDeliveryLegs, IScopeResolver scopeResolver)
   {
      _inProcessTeventPublisher = inProcessTeventPublisher;
      _teventObservationDispatcher = teventObservationDispatcher;
      _exactlyOnceDeliveryLeg = exactlyOnceDeliveryLegs.SingleOrDefault();
      _transientDeliveryLeg = transientDeliveryLegs.SingleOrDefault();
      _scopeResolver = scopeResolver;
   }

   public void Publish(ITevent tevent)
   {
      var wrappedTevent = PublisherIdentifyingTevent.Wrapped(tevent);
      var remoteDelivery = RemoteDeliveryFor(wrappedTevent);
      _inProcessTeventPublisher.Publish(wrappedTevent, _scopeResolver);
      //Observation fires at publish time, outside the publisher's transaction - the observers of a locally published tevent hear it even if that transaction later rolls back.
      _teventObservationDispatcher.Dispatch(wrappedTevent);
      remoteDelivery?.Invoke();
   }

   ///<summary>The remote delivery the tevent's declared contract selects — resolved and validated before participation dispatches,<br/>
   /// so an invalid or unroutable publish fails before any handler runs. Null when the tevent travels by participation alone:<br/>
   /// its type declares no remotability, or the endpoint wires no remote delivery at all.</summary>
   Action? RemoteDeliveryFor(IPublisherTevent<ITevent> wrappedTevent)
   {
      switch(wrappedTevent)
      {
         case IPublisherTevent<IExactlyOnceTevent> exactlyOnceTevent:
            if(_exactlyOnceDeliveryLeg is not {} exactlyOnceDeliveryLeg)
               return NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(exactlyOnceTevent.Tevent, unwiredLeg: "the exactly-once delivery leg (the outbox, wired by exactly-once Tessaging on a database-backed foundation)");
            TessageInspector.AssertValidToSendRemote(exactlyOnceTevent.Tevent);
            return () => exactlyOnceDeliveryLeg.PublishTransactionally(exactlyOnceTevent);
         case IPublisherTevent<IRemotableTevent> transientTevent:
            if(_transientDeliveryLeg is not {} transientDeliveryLeg)
               return NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(transientTevent.Tevent, unwiredLeg: "the transient delivery leg (wired by transient and exactly-once Tessaging alike)");
            TessageInspector.AssertValidToSendRemote(transientTevent.Tevent);
            return () => transientDeliveryLeg.PublishBestEffort(transientTevent);
         default:
            return null; //The tevent's type declares no remotability: participation is all the delivery there is.
      }
   }

   ///<summary>Zero wired legs is the deliberately in-process composition — no subscriber outside this process can exist, so<br/>
   /// participation already delivers the tevent's full guarantee and the missing leg is vacuous. On an endpoint that wires ANY<br/>
   /// remote delivery, subscribers may be remote, so a tevent whose contract demands a leg the endpoint did not wire is a loud<br/>
   /// publish error: silently degrading a delivery guarantee is data loss dressed as success.</summary>
   Action? NoRemoteDeliveryOnThisDeliberatelyInProcessEndpoint(ITevent tevent, string unwiredLeg)
   {
      State.Assert(_exactlyOnceDeliveryLeg == null && _transientDeliveryLeg == null,
                   () => $"Publishing {tevent.GetType().FullName} demands {unwiredLeg}, which this endpoint does not wire — even though it wires other remote tevent delivery, so its subscribers may be remote. A tevent whose contract needs a delivery leg the endpoint did not wire is a publish error, never a silent downgrade: wire the missing leg, or publish a tevent whose type does not demand it.");
      return null;
   }
}
