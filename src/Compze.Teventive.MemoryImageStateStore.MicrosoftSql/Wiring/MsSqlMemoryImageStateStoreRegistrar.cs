using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Sql.MicrosoftSql.Wiring._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Layer = Compze.Teventive.MemoryImageStateStore.MicrosoftSql._private.MsSqlMemoryImageStateStoreSqlLayer;

namespace Compze.Teventive.MemoryImageStateStore.MicrosoftSql.Wiring;

///<summary>Registers the SQL of a memory image state store kept in a SQL Server domain database's tables.</summary>
public static class MsSqlMemoryImageStateStoreRegistrar
{
   ///<summary>Registers, for a memory image state store composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>, the SQL of its<br/>
   /// tables in the SQL Server domain database this container declares, with <c>MsSqlDomainDatabase(...)</c>, and contributes the<br/>
   /// tables to the database's schema.</summary>
   ///<remarks>review-naming: MsSqlMemoryImageStateStoreSqlLayer, after MsSqlDocumentDbSqlLayer and MsSqlTeventStoreSqlLayer (state-store-in-tables, 2026-11-24).</remarks>
   public static IComponentRegistrar MsSqlMemoryImageStateStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(Singleton.For<IMemoryImageStateStoreSqlLayer>()
                                  .CreatedBy((IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) => new Layer(connectionPool, schemaManager)));
}
