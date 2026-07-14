using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
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
   readonly ITestingEndpointHost _senderHost;
   readonly ITestingEndpointHost _receiverHost;
   readonly IEndpoint _senderEndpoint;
   readonly IThreadGate _receivedTommandGate = IThreadGate.NewOpen(WaitTimeout.Seconds(1), "receivedTommand");

   public Given_two_hosts_sharing_an_interprocess_registry_where_the_receiving_host_starts_last()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);

      //No testing host features: each endpoint is composed by hand so that the shared interprocess registry - not the
      //testing host's static endpoint list - is how the endpoints find each other, exactly as separate processes would.
      _senderHost = TestingEndpointHost.Create();
      _senderEndpoint = _senderHost.RegisterEndpoint("Sender", new EndpointId(Guid.NewGuid()), ComposeEndpointDiscoveredThroughTheRegistry);

      _receiverHost = TestingEndpointHost.Create();
      _receiverHost.RegisterEndpoint("Receiver",
                                     new EndpointId(Guid.NewGuid()),
                                     builder =>
                                     {
                                        ComposeEndpointDiscoveredThroughTheRegistry(builder);
                                        builder.RegisterTessagingHandlers.ForTommand<TommandDiscoveredThroughReconciliation>(_ => _receivedTommandGate.AwaitPassThrough());
                                     });
   }

   void ComposeEndpointDiscoveredThroughTheRegistry(IEndpointBuilder builder)
   {
      builder.TypeMapper.RegisterIntegrationTestTypeMappings();
      builder.Registrar
             .CurrentTestsEndpointTransport()
             .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());
      builder.AddDistributedTessaging().ParticipateIn(_registry);
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

   [PCT] public void a_tommand_sent_by_the_sender_reaches_the_receivers_handler_once_reconciliation_has_discovered_it()
   {
      //Until the sender's reconciliation loop has discovered the receiver, the handler is unknown and sending fails loud -
      //the stated contract. The retry loop rides that loudness until discovery completes.
      var retryDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
      while(true)
      {
         try
         {
            _senderEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new TommandDiscoveredThroughReconciliation()));
            break;
         }
#pragma warning disable CA1031 //Retrying until the reconciliation loop discovers the receiver; past the deadline the filter is false and the real exception propagates.
         catch(Exception) when(DateTime.UtcNow < retryDeadline)
#pragma warning restore CA1031
         {
            Thread.Sleep(100);
         }
      }

      _receivedTommandGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   internal class TommandDiscoveredThroughReconciliation : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
