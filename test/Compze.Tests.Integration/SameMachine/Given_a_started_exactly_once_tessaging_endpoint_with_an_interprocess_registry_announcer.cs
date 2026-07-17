using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.SameMachine;
using Compze.Must;

using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.SameMachine;

///<summary>The endpoint announces where it listens once every endpoint in the host has finished starting to listen, and retracts the<br/>
/// announcement as the first act of the host's stopping — so the registry only ever lists addresses that are actually listening and fully ready.</summary>
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
      _endpoint = _host.RegisterExactlyOnceEndpoint("AnnouncingEndpoint",
                                                    new EndpointId(Guid.NewGuid()),
                                                    endpoint => endpoint.AnnounceAddressTo(_registry));
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
