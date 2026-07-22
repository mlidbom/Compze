using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Teventive.TeventStore.Typermedia;

namespace Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public abstract partial class EndpointHostTestBase
{
   ///<summary>The Backend endpoint: the fixture's taggregate tevent store and a gated handler of every tessage kind, so specs<br/>
   /// script timing through the fixture's thread gates. Identity fixed, not generated: an endpoint keeps its identity — and<br/>
   /// thereby its pooled database — across host rebuilds, which is what lets specs script an endpoint's restart.</summary>
   protected class BackendEndpointDeclaration : ExactlyOnceEndpointDeclaration<BackendEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Backend";
      public static EndpointId Id => new(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6"));

      readonly EndpointHostTestBase _fixture;
      internal BackendEndpointDeclaration(EndpointHostTestBase fixture) => _fixture = fixture;

      //Short deliberately: every send these specs expect to succeed binds instantly (a live or sole-remembered handler),
      //so the only sends that wait are the ones pinning the patience-exhausted failures - which would otherwise wait out
      //the 30s default. Specs that need a wait to SUCCEED compose their own endpoints with their own patience.
      protected override TimeSpan? HandlerAvailabilityPatience => TimeSpan.FromMilliseconds(500);

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireCommonTestTypeMappings();

      //Exactly-once kinds are async end to end, so their handlers are declared async; the gates themselves are synchronous, so the bodies complete their tasks synchronously.
      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommand _) =>
          {
             _fixture.MyExactlyOnceTommandHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          });

      protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
         .ForTevent((IMyExactlyOnceTevent _) =>
          {
             _fixture.TeventHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          })
         .ForTevent((IMyTaggregateTevent _) =>
          {
             _fixture.MyLocalTaggregateTeventHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          });

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((IMyBestEffortTevent _) => _fixture.MyBestEffortTeventLocalHandlerThreadGate.AwaitPassThrough());

      protected override void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) => handle
         .ForTommand((MyCreateTaggregateTommand tommand, ILocalTypermediaNavigatorSession navigator) =>
          {
             _fixture.MyCreateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
             MyTaggregate.Create(tommand.TaggregateId, navigator);
          })
         .ForTommand((MyUpdateTaggregateTommand tommand, ILocalTypermediaNavigatorSession navigator) =>
          {
             _fixture.MyUpdateTaggregateTommandHandlerThreadGate.AwaitPassThrough();
             navigator.Execute(new TeventStoreApi().Tueries.GetForUpdate<MyTaggregate>(tommand.TaggregateId)).Update();
          })
         .ForTommand((MyAtMostOnceTypermediaTommandWithResult _) =>
          {
             _fixture.TommandHandlerWithResultThreadGate.AwaitPassThrough();
             return new MyTommandResult();
          });

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((MyTuery _) =>
          {
             _fixture.TueryHandlerThreadGate.AwaitPassThrough();
             return new MyTueryResult();
          });

      //Observation - the transaction-ignoring subscription kind: the Backend's own locally published tevents are queued for
      //this observer when their publishing unit of work commits, and dispatched off-thread.
      protected override void ObserveTevents(ITeventObservationRegistrar observe) => observe
         .ForTevent((IMyTaggregateTevent _) => _fixture.MyTaggregateTeventBackendObserverThreadGate.AwaitPassThrough());

      protected override void Declare(ExactlyOnceEndpointBuilder endpoint) =>
         endpoint.RegisterTeventStore()
                 .HandleTaggregate<MyTaggregate, IMyTaggregateTevent>();
   }

   ///<summary>The Remote endpoint: the Backend's conversing peer. Identity fixed for the same reason as<br/>
   /// <see cref="BackendEndpointDeclaration"/>. The constructor flags script deployments where the endpoint returns with a<br/>
   /// shrunk advertisement: no longer handling its tommand, or having renounced its tevent subscriptions (see the<br/>
   /// advertisement lifecycle in <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
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

   ///<summary>The successor to the Remote endpoint — deliberately a NEW identity: a blue/green replacement is a different<br/>
   /// endpoint that advertises the same tommand type, never the old identity reused.</summary>
   protected class RemoteSuccessorEndpointDeclaration : ExactlyOnceEndpointDeclaration<RemoteSuccessorEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "RemoteSuccessor";
      public static EndpointId Id => new(Guid.Parse("46ECC3A4-5657-4A0A-9C78-9FEEA5A1010D"));

      readonly EndpointHostTestBase _fixture;
      internal RemoteSuccessorEndpointDeclaration(EndpointHostTestBase fixture) => _fixture = fixture;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireCommonTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommandHandledByTheRemoteEndpoint _) =>
          {
             _fixture.RemoteSuccessorTommandHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }
}
