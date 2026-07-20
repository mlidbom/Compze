using Compze.Tessaging.Endpoints;
using Compze.Must;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>Readiness — the explicit awaitable: an application awaits, at a moment of its own choosing, "complete when this<br/>
/// endpoint can reach handlers for these types" (<see cref="IEndpoint.AwaitReadinessAsync"/>), front-loading the startup<br/>
/// discovery wait that a waiting send would otherwise make the first unlucky caller pay. A handler is reachable when the<br/>
/// endpoint itself serves the type, or when a send of the type would proceed without waiting; exhausted patience throws<br/>
/// <see cref="EndpointNotReadyWithinPatienceException"/> naming every type still unavailable<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
public class Given_an_endpoint_awaiting_readiness : UniversalTestBase
{
   static readonly EndpointId AwaitingEndpointId = new(Guid.Parse("C5D91E37-8A46-4B02-9F58-1E74A6C0D823"));
   static readonly EndpointId LateHandlerEndpointId = new(Guid.Parse("7B30F8C2-46D5-4E19-A8B7-5C92E1D46F30"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _awaitingEndpoint;

   public Given_an_endpoint_awaiting_readiness()
   {
      _host = TestingEndpointHost.Create();
      _awaitingEndpoint = _host.RegisterExactlyOnceEndpoint(
         "Awaiting",
         AwaitingEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            //The endpoint's own roster serves these two - what the readiness-for-own-types specification awaits.
            .RegisterTessageHandlers(handle => handle
                       .ForTommand((MyExactlyOnceTommand _) => Task.CompletedTask)
                       .ForTuery((MyTuery _) => new MyTueryResult())));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task readiness_awaited_before_the_serving_endpoint_exists_completes_when_it_is_met()
   {
      //The await begins now: nothing this endpoint has ever met serves either type - the serving endpoint does not even exist yet.
      var readinessTask = _awaitingEndpoint.AwaitReadinessAsync(
         ReadinessTypes.These(typeof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint), typeof(MyTueryHandledOnlyByTheLateEndpoint)));

      await StartTheLateHandlerEndpointAsync();

      await readinessTask;
   }

   [PCT] public async Task readiness_for_types_the_endpoints_own_roster_serves_completes_without_any_other_endpoint() =>
      //Short patience discriminates: if own-roster types were not counted available, this await would exhaust it and throw.
      await _awaitingEndpoint.AwaitReadinessAsync(ReadinessTypes.These(typeof(MyExactlyOnceTommand), typeof(MyTuery)), patience: TimeSpan.FromMilliseconds(100));

   [PCT] public async Task readiness_patience_exhausted_throws_naming_every_type_still_unavailable()
   {
      var message = (await InvokingAsync(() => _awaitingEndpoint.AwaitReadinessAsync(
                        ReadinessTypes.These(typeof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint), typeof(MyTueryHandledOnlyByTheLateEndpoint)),
                        patience: TimeSpan.FromMilliseconds(100)))
                    .Must().ThrowAsync<EndpointNotReadyWithinPatienceException>()).Which.Message;

      message.Must().Contain(nameof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint));
      message.Must().Contain(nameof(MyTueryHandledOnlyByTheLateEndpoint));
      message.Must().Contain("nothing this endpoint has ever met serves it");
   }

   ///<summary>Composes and starts the endpoint serving the late-handled types — after the host's phase barrier already ran,<br/>
   /// so the specification drives the late endpoint's own lifecycle phases: the same phases, in the same order. Its<br/>
   /// announcement is what the readiness await is spent waiting for.</summary>
   async Task StartTheLateHandlerEndpointAsync()
   {
      var lateHandlerEndpoint = _host.RegisterExactlyOnceEndpoint(
         "LateHandler",
         LateHandlerEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTommand((MyExactlyOnceTommandHandledOnlyByTheLateEndpoint _) => Task.CompletedTask)
                       .ForTuery((MyTueryHandledOnlyByTheLateEndpoint _) => new MyTueryResult())));
      await lateHandlerEndpoint.StartAsync();
   }
}

public class MyTueryHandledOnlyByTheLateEndpoint : Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
