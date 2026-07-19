using Compze.Tessaging.Endpoints;
using Compze.Abstractions.Time.Public;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>A crashed process never releases its process lease — the lease just stops being refreshed. The endpoint's next<br/>
/// process must not fail loud on that corpse: a lease unrefreshed for a whole lease duration is stale, and a starting<br/>
/// endpoint takes it over silently. That is crash recovery, not conflict — restarting after a crash needs no manual cleanup.</summary>
public class Given_a_domain_database_remembering_a_crashed_processes_lease : UniversalTestBase
{
   static readonly EndpointId RebornEndpointId = new(Guid.Parse("3F8A61D2-5C49-4B70-A8E3-16D9F2B75C08"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _rebornEndpoint;

   public Given_a_domain_database_remembering_a_crashed_processes_lease()
   {
      _host = TestingEndpointHost.Create();
      _rebornEndpoint = _host.RegisterExactlyOnceEndpoint(
         "Reborn",
         RebornEndpointId,
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings()));
   }

   //The crash is scripted through the catalog sql layer: the crashed predecessor's entry, its lease held and last
   //heartbeated an hour ago - exactly what a killed process leaves behind.
   protected override async Task InitializeAsyncInternal()
   {
      var catalog = _rebornEndpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IEndpointCatalogSqlLayer>();
      await catalog.InitAsync();
      (await catalog.TryInsertEntryHoldingTheLeaseAsync(
          "Reborn",
          RebornEndpointId,
          leaseHolderId: Guid.NewGuid(),
          leaseHolderDescription: "process 4242 on CrashedHost",
          utcNow: UtcTimeSource.UtcNow - TimeSpan.FromHours(1))).Must().BeTrue();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task the_endpoint_takes_over_the_stale_lease_and_starts()
   {
      await _host.StartAsync();
      _rebornEndpoint.IsRunning.Must().BeTrue();
   }
}
