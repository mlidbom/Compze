using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.Sqlite._internal;
using Compze.Sql.Sqlite.Wiring._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Layer = Compze.Teventive.MemoryImageStateStore.Sqlite._private.SqliteMemoryImageStateStoreSqlLayer;

namespace Compze.Teventive.MemoryImageStateStore.Sqlite.Wiring;

///<summary>Registers the SQL of a memory image state store kept in a SQLite domain database's tables.</summary>
public static class SqliteMemoryImageStateStoreRegistrar
{
   ///<summary>Registers, for a memory image state store composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>, the SQL of its<br/>
   /// tables in the SQLite domain database this container declares, such as with <c>SqliteDomainDatabase(...)</c>, and contributes<br/>
   /// the tables to the database's schema.</summary>
   public static IComponentRegistrar SqliteMemoryImageStateStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteDatabaseProcessLocks()
               .SqliteSchemaContribution(Layer.SchemaCreationSql)
               .Register(Singleton.For<IMemoryImageStateStoreSqlLayer>()
                                  .CreatedBy((ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks) => new Layer(connectionPool, schemaManager, processLocks)));
}
