using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.PostgreSql.Wiring._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Layer = Compze.Teventive.MemoryImageStateStore.PostgreSql._private.PgSqlMemoryImageStateStoreSqlLayer;

namespace Compze.Teventive.MemoryImageStateStore.PostgreSql.Wiring;

///<summary>Registers the SQL of a memory image state store kept in a PostgreSQL domain database's tables.</summary>
public static class PgSqlMemoryImageStateStoreRegistrar
{
   ///<summary>Registers, for a memory image state store composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>, the SQL of its<br/>
   /// tables in the PostgreSQL domain database this container declares, with <c>PgSqlDomainDatabase(...)</c>, and contributes the<br/>
   /// tables to the database's schema.</summary>
   public static IComponentRegistrar PgSqlMemoryImageStateStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlDatabaseProcessLocks()
               .PgSqlSchemaContribution(Layer.SchemaCreationStatements)
               .Register(Singleton.For<IMemoryImageStateStoreSqlLayer>()
                                  .CreatedBy((IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks) => new Layer(connectionPool, schemaManager, processLocks)));
}
