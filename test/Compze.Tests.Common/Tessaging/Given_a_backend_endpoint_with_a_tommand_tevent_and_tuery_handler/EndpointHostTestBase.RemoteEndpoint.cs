using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;

namespace Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public abstract partial class EndpointHostTestBase
{
   ///<summary>The Remote endpoint: the Backend's conversing peer. Identity fixed for the same reason as<br/>
   /// <see cref="BackendEndpointDeclaration"/>. The constructor flags script deployments where the endpoint returns with a<br/>
   /// shrunk advertisement: no longer handling its tommand, or having renounced its tevent subscriptions (see the<br/>
   /// advertisement lifecycle in <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
   protected class RemoteEndpointDeclaration : ExactlyOnceEndpointDeclaration<RemoteEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Remote";
      public static EndpointId Id => new(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B"));

      readonly EndpointHostTestBase _fixture;
      readonly bool _withItsTommandHandler;
      readonly bool _withItsTeventSubscriptions;

      internal RemoteEndpointDeclaration(EndpointHostTestBase fixture, bool withItsTommandHandler = true, bool withItsTeventSubscriptions = true)
      {
         _fixture = fixture;
         _withItsTommandHandler = withItsTommandHandler;
         _withItsTeventSubscriptions = withItsTeventSubscriptions;
      }

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireCommonTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle)
      {
         if(!_withItsTommandHandler) return;

         handle.ForTommand((MyExactlyOnceTommandHandledByTheRemoteEndpoint _) =>
         {
            _fixture.MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassThrough();
            return Task.CompletedTask;
         });
      }

      protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle)
      {
         if(!_withItsTeventSubscriptions) return;

         handle.ForTevent((IMyTaggregateTevent _) =>
                {
                   _fixture.MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassThrough();
                   return Task.CompletedTask;
                })
               //Publisher-conscious subscription: subscribing to the taggregate's wrapper type receives the wrapped tevent as MyTaggregate published it.
               .ForTevent((IMyTaggregateTevent<IMyTaggregateTevent> _) =>
                {
                   _fixture.MyRemotePublisherConsciousTeventHandlerThreadGate.AwaitPassThrough();
                   return Task.CompletedTask;
                });
      }

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle)
      {
         if(!_withItsTeventSubscriptions) return;

         handle.ForTevent((IMyBestEffortTevent tevent) =>
         {
            _fixture.RemotelyReceivedBestEffortTevents.Enqueue(tevent);
            _fixture.MyBestEffortTeventRemoteHandlerThreadGate.AwaitPassThrough();
         });
      }

      //Observation - the transaction-ignoring subscription kind: an arriving tevent is queued for these observers on
      //arrival (it is already a committed fact on its publisher), before and outside the transactional handling above.
      protected override void ObserveTevents(ITeventObservationRegistrar observe)
      {
         if(!_withItsTeventSubscriptions) return;

         observe.ForTevent((IMyTaggregateTevent _) => _fixture.MyTaggregateTeventRemoteObserverThreadGate.AwaitPassThrough())
                .ForTevent((IMyBestEffortTevent _) => _fixture.MyBestEffortTeventRemoteObserverThreadGate.AwaitPassThrough());
      }
   }
}
