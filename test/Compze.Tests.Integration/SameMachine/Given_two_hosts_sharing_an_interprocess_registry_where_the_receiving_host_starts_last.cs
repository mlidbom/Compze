using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.TessageTypes;
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

      //Production hosts, and the shared interprocess registry - not a testing host's own registry - is how the endpoints
      //find each other, exactly as separate processes would.
      _senderHost = EndpointHost.Production.Create(CreateEndpointContainerBuilder, new EnvironmentParticipatingInTheSharedRegistry(_registry));
      _senderEndpoint = _senderHost.RegisterEndpoint(new SenderEndpointDeclaration());

      _receiverHost = EndpointHost.Production.Create(CreateEndpointContainerBuilder, new EnvironmentParticipatingInTheSharedRegistry(_registry));
      _receiverHost.RegisterEndpoint(new ReceiverEndpointDeclaration(this));
   }

   static IContainerBuilder CreateEndpointContainerBuilder() =>
      TestEnv.DIContainer.CreateTestingContainerBuilder()._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer());

   ///<summary>The current test's transport and domain-database binding, plus participation in the specification's shared<br/>
   /// interprocess registry — the discovery this specification is about.</summary>
   class EnvironmentParticipatingInTheSharedRegistry : IEndpointEnvironment
   {
      readonly InterprocessEndpointRegistry _registry;
      internal EnvironmentParticipatingInTheSharedRegistry(InterprocessEndpointRegistry registry) => _registry = registry;

      public void DeclareOn<TConcreteBuilder>(EndpointBuilder<TConcreteBuilder> endpointBuilder) where TConcreteBuilder : EndpointBuilder<TConcreteBuilder>
      {
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
         endpointBuilder.ParticipateIn(_registry);
      }

      public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) =>
         endpointBuilder.ConfigurePersistence(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpointBuilder.Configuration.Id.ToString()));
   }

   class SenderEndpointDeclaration : ExactlyOnceEndpointDeclaration<SenderEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Sender";
      public static EndpointId Id { get; } = new(Guid.Parse("DE099D03-3D85-4225-8BEA-D567846AB792"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   class ReceiverEndpointDeclaration : ExactlyOnceEndpointDeclaration<ReceiverEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Receiver";
      public static EndpointId Id { get; } = new(Guid.Parse("90127B1C-2630-4C2E-969E-1E22D9D594A7"));

      readonly Given_two_hosts_sharing_an_interprocess_registry_where_the_receiving_host_starts_last _specification;
      internal ReceiverEndpointDeclaration(Given_two_hosts_sharing_an_interprocess_registry_where_the_receiving_host_starts_last specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandDiscoveredThroughReconciliation _) =>
          {
             _specification._receivedTommandGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
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

   internal class TommandDiscoveredThroughReconciliation : Remotable.ExactlyOnce.Tommand;
}
