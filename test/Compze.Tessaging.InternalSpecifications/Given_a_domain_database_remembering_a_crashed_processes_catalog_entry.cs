using Compze.Tessaging.Endpoints;
using Compze.Abstractions.Time;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
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
   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _rebornEndpoint;

   public Given_a_domain_database_remembering_a_crashed_processes_catalog_entry()
   {
      _host = TestingEndpointHost.Create();
      _rebornEndpoint = _host.RegisterEndpoint(new RebornEndpointDeclaration());
   }

   class RebornEndpointDeclaration : ExactlyOnceEndpointDeclaration<RebornEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Reborn";
      public static EndpointId Id => new(Guid.Parse("3F8A61D2-5C49-4B70-A8E3-16D9F2B75C08"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();
   }

   //The crash is scripted through the catalog sql layer: a predecessor took the process lock - which stamps its holder into
   //the entry - and then vanished. Disposing its hold reproduces exactly what a killed process leaves behind: the lock
   //released by the infrastructure, but its catalog entry and recorded lock-holder description still in place.
   protected override async Task InitializeAsyncInternal()
   {
      var catalog = _rebornEndpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IEndpointCatalogSqlLayer>();
      await catalog.InitAsync();
      (await catalog.TryInsertEntryAsync(RebornEndpointDeclaration.Name, RebornEndpointDeclaration.Id, utcNow: UtcTimeSource.UtcNow - TimeSpan.FromHours(1))).Must().BeTrue();

      var crashedPredecessorHold = await catalog.TryTakeProcessLockAsync(RebornEndpointDeclaration.Name, "process 4242 on CrashedHost", _ => {});
      crashedPredecessorHold.Must().NotBeNull();
      await crashedPredecessorHold!.DisposeAsync();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task the_endpoint_claims_the_free_lock_and_starts()
   {
      await _host.StartAsync();
      _rebornEndpoint.IsRunning.Must().BeTrue();
   }
}
