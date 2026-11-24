using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite.Wiring;
using Compze.Teventive.MemoryImageStateStore.MicrosoftSql.Wiring;
using Compze.Teventive.MemoryImageStateStore.MySql.Wiring;
using Compze.Teventive.MemoryImageStateStore.PostgreSql.Wiring;
using Compze.Teventive.MemoryImageStateStore.Sqlite.Wiring;
using Compze.Internals.Testing;

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>Declares a domain database of the current test's persistence layer, and the memory image state store's tables in it, the way a<br/>
/// consumer declares them: the database's public registrar extension, then the store's SQL layer for it.</summary>
static class MemoryImageStateStoreInTablesTestingRegistrar
{
   internal static IComponentRegistrar CurrentTestsDomainDatabaseWithTheMemoryImageStateStoresSqlLayer(this IComponentRegistrar registrar, string connectionStringName) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql        => registrar.MsSqlDomainDatabase(connectionStringName).MsSqlMemoryImageStateStoreSqlLayer(),
         SqlLayer.MySql        => registrar.MySqlDomainDatabase(connectionStringName).MySqlMemoryImageStateStoreSqlLayer(),
         SqlLayer.PgSql        => registrar.PgSqlDomainDatabase(connectionStringName).PgSqlMemoryImageStateStoreSqlLayer(),
         SqlLayer.Sqlite       => registrar.SqliteDomainDatabase(connectionStringName).SqliteMemoryImageStateStoreSqlLayer(),
         SqlLayer.SqliteMemory => registrar.SqliteMemoryDomainDatabase(connectionStringName).SqliteMemoryImageStateStoreSqlLayer(),
         _                     => throw new ArgumentOutOfRangeException()
      };
}
