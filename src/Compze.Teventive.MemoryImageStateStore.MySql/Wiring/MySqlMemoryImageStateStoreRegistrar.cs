using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MySql._internal;
using Compze.Sql.MySql.Wiring._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Layer = Compze.Teventive.MemoryImageStateStore.MySql._private.MySqlMemoryImageStateStoreSqlLayer;

namespace Compze.Teventive.MemoryImageStateStore.MySql.Wiring;

///<summary>Registers the SQL of a memory image state store kept in a MySQL domain database's tables.</summary>
public static class MySqlMemoryImageStateStoreRegistrar
{
   ///<summary>Registers, for a memory image state store composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>, the SQL of its<br/>
   /// tables in the MySQL domain database this container declares, with <c>MySqlDomainDatabase(...)</c>, and contributes the<br/>
   /// tables to the database's schema.</summary>
   public static IComponentRegistrar MySqlMemoryImageStateStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(Singleton.For<IMemoryImageStateStoreSqlLayer>()
                                  .CreatedBy((IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) => new Layer(connectionPool, schemaManager)));
}
