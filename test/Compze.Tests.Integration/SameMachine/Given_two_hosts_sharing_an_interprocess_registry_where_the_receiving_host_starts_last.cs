using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The tessaging router keeps reconciling its connections with the registry's membership: an endpoint that appears — here in a<br/>
/// host started AFTER the sender's host, so the sender's startup connection pass could not have seen it — is discovered and connected<br/>
/// to, and until it is discovered, sending to it fails loud rather than queueing for an unknown handler.</summary>
public class Given_two_hosts_sharing_an_interprocess_registry_where_the_receiving_host_starts_last : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registry;
   readonly IEndpointHost _senderHost;
   readonly IEndpointHost _receiverHost;
   readonly ExactlyOnceEndpoint _senderEndpoint;
   readonly IThreadGate _receivedTommandGate = IThreadGate.NewOpen(WaitTimeout.Seconds(1), "receivedTommand");

   public Given_two_hosts_sharing_an_interprocess_registry_where_the_receiving_host_starts_last()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);

      //The production host, and each endpoint composed by hand: the shared interprocess registry - not a testing host's own
      //registry - is how the endpoints find each other, exactly as separate processes would.
      _senderHost = EndpointHost.Production.Create(CreateEndpointContainerBuilder);
      _senderEndpoint = _senderHost.RegisterEndpoint(container => ExactlyOnceEndpoint.Build(
         container, "Sender", new EndpointId(Guid.NewGuid()),
         ComposeEndpointDiscoveredThroughTheRegistry));

      _receiverHost = EndpointHost.Production.Create(CreateEndpointContainerBuilder);
      _receiverHost.RegisterEndpoint(container => ExactlyOnceEndpoint.Build(
         container, "Receiver", new EndpointId(Guid.NewGuid()),
         endpointBuilder =>
         {
            ComposeEndpointDiscoveredThroughTheRegistry(endpointBuilder);
            endpointBuilder.RegisterTessageHandlers(handle => handle.ForTommand((TommandDiscoveredThroughReconciliation _) =>
            {
               _receivedTommandGate.AwaitPassThrough();
               return Task.CompletedTask;
            }));
         }));
   }

   static IContainerBuilder CreateEndpointContainerBuilder() =>
      TestEnv.DIContainer.CreateTestingContainerBuilder()._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer());

   void ComposeEndpointDiscoveredThroughTheRegistry(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      endpointBuilder
         .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
         .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
         .ConfigurePersistence(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpointBuilder.Configuration.Id.ToString()))
         .ParticipateIn(_registry);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _senderHost.StartAsync();
      await _receiverHost.StartAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _senderHost.DisposeAsync();
      await _receiverHost.DisposeAsync();
      _registry.Delete();
      _registry.Dispose();
   }

   [PCT] public async Task a_tommand_sent_by_the_sender_reaches_the_receivers_handler_once_reconciliation_has_discovered_it()
   {
      //Until the sender's reconciliation loop has discovered the receiver, the handler is unknown and sending fails loud -
      //the stated contract. The retry loop rides that loudness until discovery completes.
      var retryDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
      while(true)
      {
         try
         {
            await _senderEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandDiscoveredThroughReconciliation());
            break;
         }
#pragma warning disable CA1031 //Retrying until the reconciliation loop discovers the receiver; past the deadline the filter is false and the real exception propagates.
         catch(Exception) when(DateTime.UtcNow < retryDeadline)
#pragma warning restore CA1031
         {
            await Task.Delay(100);
         }
      }

      _receivedTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   internal class TommandDiscoveredThroughReconciliation : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
