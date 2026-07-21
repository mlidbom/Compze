using Compze.Tessaging.Endpoints;
using Compze.Abstractions.Time;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>A crashed process never cleans up its catalog bookkeeping: its process lock is released by the infrastructure —<br/>
/// the database session or OS lock dies with the process — but its catalog entry, recorded lock-holder description<br/>
/// included, lingers. The endpoint's next process must not fail loud on that corpse: the lock, not the bookkeeping,<br/>
/// decides — a free lock is claimed immediately, with no waiting and no manual cleanup.</summary>
public class Given_a_domain_database_remembering_a_crashed_processes_catalog_entry : UniversalTestBase
{
   static readonly EndpointId RebornEndpointId = new(Guid.Parse("3F8A61D2-5C49-4B70-A8E3-16D9F2B75C08"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _rebornEndpoint;

   public Given_a_domain_database_remembering_a_crashed_processes_catalog_entry()
   {
      _host = TestingEndpointHost.Create();
      _rebornEndpoint = _host.RegisterExactlyOnceEndpoint(
         "Reborn",
         RebornEndpointId,
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireTessagingInternalSpecificationTypeMappings()));
   }

   //The crash is scripted through the catalog sql layer: the crashed predecessor's entry with its recorded lock holder
   //still in place - exactly what a killed process leaves behind, its lock long since released by the infrastructure.
   protected override async Task InitializeAsyncInternal()
   {
      var catalog = _rebornEndpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IEndpointCatalogSqlLayer>();
      await catalog.InitAsync();
      (await catalog.TryInsertEntryAsync("Reborn", RebornEndpointId, utcNow: UtcTimeSource.UtcNow - TimeSpan.FromHours(1))).Must().BeTrue();
      await catalog.RecordLockHolderAsync("Reborn", "process 4242 on CrashedHost");
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task the_endpoint_claims_the_free_lock_and_starts()
   {
      await _host.StartAsync();
      _rebornEndpoint.IsRunning.Must().BeTrue();
   }
}
