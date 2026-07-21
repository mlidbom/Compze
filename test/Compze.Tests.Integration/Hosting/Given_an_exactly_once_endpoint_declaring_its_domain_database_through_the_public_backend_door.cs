using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.Wiring;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.DocumentDb.MySql.Wiring;
using Compze.DocumentDb.PostgreSql.Wiring;
using Compze.DocumentDb.Sqlite.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.MySql.Wiring;
using Compze.Tessaging.PostgreSql.Wiring;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Teventive.TeventStore.MySql.Wiring;
using Compze.Teventive.TeventStore.PostgreSql.Wiring;
using Compze.Teventive.TeventStore.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// The consumer-proof for the domain-database doors: an exactly-once endpoint whose persistence is declared exactly the way a
/// consumer composes it — the current backend's public registrar door (<c>MsSqlDomainDatabase(...)</c>,
/// <c>MySqlDomainDatabase(...)</c>, <c>PgSqlDomainDatabase(...)</c>, <c>SqliteDomainDatabase(...)</c>,
/// <c>SqliteMemoryDomainDatabase(...)</c>) followed by the feature sql layers, never the testing hosts' backend switch. The
/// endpoint then converses with itself, proving the whole durable vertical — schema creation included — stands on the
/// door-declared pool.
///</summary>
public class Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_door : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   bool _tommandHandled;

   public Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_door()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()));

      _endpoint = _host.RegisterEndpoint(container => ExactlyOnceEndpoint.Build(
         container,
         "PublicDoorDomainDatabase",
         new EndpointId(Guid.Parse("c4a91b3e-7d20-45f8-9b6a-2e8d51c30f74")),
         endpointBuilder =>
         {
            endpointBuilder
               .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
               .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
               .ConfigurePersistence(registrar => DeclareDomainDatabaseThroughThePublicDoor(registrar, connectionStringName: endpointBuilder.Configuration.Id.ToString()))
               .RegisterTessageBusHandlers(handle => handle
                       .ForTommand((TommandProvingTheDoorDeclaredPersistenceWorks _) =>
                        {
                           _tommandHandled = true;
                           return Task.CompletedTask;
                        }));
         }));
   }

   static IComponentRegistrar DeclareDomainDatabaseThroughThePublicDoor(IComponentRegistrar registrar, string connectionStringName) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => registrar.MsSqlDomainDatabase(connectionStringName)
                                    .MsSqlDocumentDbSqlLayer()
                                    .MsSqlTessagingSqlLayer()
                                    .MsSqlTeventStoreSqlLayer(),
         SqlLayer.MySql => registrar.MySqlDomainDatabase(connectionStringName)
                                    .MySqlDocumentDbSqlLayer()
                                    .MySqlTessagingSqlLayer()
                                    .MySqlTeventStoreSqlLayer(),
         SqlLayer.PgSql => registrar.PgSqlDomainDatabase(connectionStringName)
                                    .PgSqlDocumentDbSqlLayer()
                                    .PgSqlTessagingSqlLayer()
                                    .PgSqlTeventStoreSqlLayer(),
         //The declaration-object interner form rides the on-disk branch so the one door that takes a SqliteDomainDatabase is consumer-proven too.
         SqlLayer.Sqlite => registrar.SqliteDomainDatabase(connectionStringName)
                                     .SqliteTypeIdInterner(new SqliteDomainDatabase($"{connectionStringName}.TypeIdInterner"))
                                     .SqliteDocumentDbSqlLayer()
                                     .SqliteTessagingSqlLayer()
                                     .SqliteTeventStoreSqlLayer(),
         SqlLayer.SqliteMemory => registrar.SqliteMemoryDomainDatabase(connectionStringName)
                                           .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                           .SqliteDocumentDbSqlLayer()
                                           .SqliteTessagingSqlLayer()
                                           .SqliteTeventStoreSqlLayer(),
         _ => throw new ArgumentOutOfRangeException()
      };

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_starts_and_runs() => _endpoint.IsRunning.Must().BeTrue();

   [PCT] public async Task a_tommand_the_endpoint_sends_itself_is_handled_through_the_door_declared_persistence()
   {
      await _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandProvingTheDoorDeclaredPersistenceWorks());
      _tommandHandled.Must().BeTrue();
   }

   protected internal class TommandProvingTheDoorDeclaredPersistenceWorks : Remotable.ExactlyOnce.Tommand;
}
