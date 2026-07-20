using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Internals.Peers;
using Compze.Tessaging.Transport.Discovery;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.TypeIdentifiers;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The cold-start half of waiting sends for exactly-once tommands: a send whose type's handler was never met does not<br/>
/// explode — it waits, within the endpoint's handler-availability patience, for the first contact, and only then binds to its<br/>
/// one specific receiver. The wait strictly precedes the bind, so the exactly-once in-order guarantee is untouched: the<br/>
/// tommand still binds exactly once, before its row is saved, and rides the bound pair's single ordered, receiver-deduped<br/>
/// delivery stream (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
public class Given_an_exactly_once_tommand_send_racing_discovery : UniversalTestBase
{
   static readonly EndpointId SenderEndpointId = new(Guid.Parse("F1A83D57-2B96-4E40-8C15-7D3E90B62A84"));
   static readonly EndpointId LateHandlerEndpointId = new(Guid.Parse("58C2E9B4-71D0-4F36-A5E8-9B14F7C3D620"));
   static readonly EndpointId RetiredPeerId = new(Guid.Parse("9E60B3A8-4C27-4D91-B7F5-2A85D1E04C36"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _senderEndpoint;
   readonly IThreadGate _lateHandlerThreadGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "LateHandlerThreadGate");

   public Given_an_exactly_once_tommand_send_racing_discovery()
   {
      _host = TestingEndpointHost.Create();
      _senderEndpoint = _host.RegisterExactlyOnceEndpoint(
         "Sender",
         SenderEndpointId,
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings()));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task A_send_before_the_handling_endpoint_was_ever_met_waits_binds_on_first_contact_and_is_delivered()
   {
      //The send begins now: nothing this endpoint has ever met serves the type - the handling endpoint does not even exist yet.
      var sendTask = _senderEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledOnlyByTheLateEndpoint());

      await StartTheLateHandlerEndpointAsync();

      await sendTask;
      _lateHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task A_send_while_several_remembered_peers_advertise_the_type_and_none_is_live_binds_to_the_one_that_connects_and_is_delivered()
   {
      //Two remembered peers advertise the type - a handler replacement whose retired peer was never decommissioned - and
      //neither is live: with no way to know which is current, the send waits instead of binding blind.
      var tommandTypeIdString = _senderEndpoint.ServiceLocator.Resolve<ITypeMap>().GetId(typeof(MyExactlyOnceTommandHandledOnlyByTheLateEndpoint)).CanonicalString;
      var senderPeerRegistry = _senderEndpoint.ServiceLocator.Resolve<IPeerRegistry>();
      await senderPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("RetiredPeer", RetiredPeerId, [tommandTypeIdString]));
      await senderPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("LateHandler", LateHandlerEndpointId, [tommandTypeIdString]));

      var sendTask = _senderEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledOnlyByTheLateEndpoint());

      //One of the remembered handlers connects: live is current by definition, so the send binds to it - the replacement
      //ambiguity is resolved by the very fact of connecting, and the retired peer's stream never sees the tommand.
      await StartTheLateHandlerEndpointAsync();

      await sendTask;
      _lateHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   ///<summary>Composes and starts the endpoint handling <see cref="MyExactlyOnceTommandHandledOnlyByTheLateEndpoint"/> — after<br/>
   /// the host's phase barrier already ran, so the specification drives the late endpoint's own lifecycle phases: the same<br/>
   /// phases, in the same order. Its announcement is what the waiting send's patience is spent waiting for.</summary>
   async Task StartTheLateHandlerEndpointAsync()
   {
      var lateHandlerEndpoint = _host.RegisterExactlyOnceEndpoint(
         "LateHandler",
         LateHandlerEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTommand((MyExactlyOnceTommandHandledOnlyByTheLateEndpoint _) =>
                        {
                           _lateHandlerThreadGate.AwaitPassThrough();
                           return Task.CompletedTask;
                        })));
      await lateHandlerEndpoint.StartAsync();
   }
}

public class MyExactlyOnceTommandHandledOnlyByTheLateEndpoint : TessageTypes.Remotable.ExactlyOnce.Tommand;
