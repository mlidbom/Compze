using Compze.Tessaging.Endpoints;
using Compze.Hosting.SameMachine;
using Compze.Must;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The endpoint announces where it listens after its own listening phase and retracts the announcement as the first<br/>
/// act of its disposal — so the registry only ever lists addresses that are actually listening and fully ready.</summary>
public class Given_a_started_exactly_once_tessaging_endpoint_with_an_interprocess_registry_announcer : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registry;
   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   bool _hostDisposed;

   public Given_a_started_exactly_once_tessaging_endpoint_with_an_interprocess_registry_announcer()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new AnnouncingEndpointDeclaration(), new EnvironmentAlsoAnnouncingTo(_host.Environment, _registry));
   }

   class AnnouncingEndpointDeclaration : ExactlyOnceEndpointDeclaration<AnnouncingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "AnnouncingEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("B25BE88A-F594-4E7A-ACA9-7D49EA8C17D8"));
   }

   ///<summary>The wrapped <see cref="IEndpointEnvironment"/> plus announcing to one more <see cref="IEndpointAddressAnnouncer"/> —<br/>
   /// here: the testing host's environment plus the specification's own separate registry.</summary>
   class EnvironmentAlsoAnnouncingTo : IEndpointEnvironment
   {
      readonly IEndpointEnvironment _environment;
      readonly IEndpointAddressAnnouncer _additionalAnnouncementTarget;

      internal EnvironmentAlsoAnnouncingTo(IEndpointEnvironment environment, IEndpointAddressAnnouncer additionalAnnouncementTarget)
      {
         _environment = environment;
         _additionalAnnouncementTarget = additionalAnnouncementTarget;
      }

      public void Configure(EndpointBuilder endpointBuilder)
      {
         _environment.Configure(endpointBuilder);
         endpointBuilder.AnnounceAddressTo(_additionalAnnouncementTarget);
      }

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) => _environment.ConfigureDomainDatabase(endpointBuilder);
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal()
   {
      await DisposeHostAsync();
      _registry.Delete();
      _registry.Dispose();
   }

   async Task DisposeHostAsync()
   {
      if(_hostDisposed) return;
      _hostDisposed = true;
      await _host.DisposeAsync();
   }

   [PCT] public void the_endpoints_address_is_announced_in_the_registry() =>
      _registry.ServerEndpointAddresses.Single().Must().Be(_endpoint.Address);

   [PCT] public async Task disposing_the_host_retracts_the_announced_address()
   {
      await DisposeHostAsync();
      _registry.ServerEndpointAddresses.Must().BeEmpty();
   }
}
