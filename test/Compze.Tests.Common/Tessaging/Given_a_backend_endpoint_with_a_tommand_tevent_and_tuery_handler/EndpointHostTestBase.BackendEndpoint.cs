using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;
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

      //Exactly-once kinds are async end to end, so their handlers pass their gates asynchronously, holding no thread while a gate keeps them waiting.
      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommand _) => _fixture.MyExactlyOnceTommandHandlerThreadGate.AwaitPassThroughAsync());

      protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
         .ForTevent((IMyExactlyOnceTevent _) => _fixture.TeventHandlerThreadGate.AwaitPassThroughAsync())
         .ForTevent((IMyTaggregateTevent _) => _fixture.MyLocalTaggregateTeventHandlerThreadGate.AwaitPassThroughAsync());

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

      //Async, so that the specifications that keep tueries waiting at the closed gate hold no thread for each of them.
      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery(async (MyTuery _, IScopeResolver _) =>
          {
             await _fixture.TueryHandlerThreadGate.AwaitPassThroughAsync();
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
}
