using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Must;
using Compze.Sql.Sqlite._internal;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Sql.InternalSpecifications.ConnectionPoolDisposal;

///<summary>A SQLite connection pool that has served a command, held by its user past the disposal of the container, which disposes<br/>
/// it: a connection it opened then would go back to Microsoft.Data.Sqlite's pool as it closed, and hold the database's file for the<br/>
/// rest of the process.</summary>
public class A_SQLite_connection_pool_used_after_its_container_disposed_it : UniversalTestBase
{
   const string DomainDatabaseName = "ConnectionPoolUsedAfterItsDisposal";
   const string OnlySqlitesPoolsAreDisposed = "Only the SQLite layers' connection pools hold anything to release, and are disposed";

   readonly DomainDatabaseOnTheCurrentSqlLayer _database = DomainDatabaseOnTheCurrentSqlLayer.Named(DomainDatabaseName);
   ISqliteConnectionPool _pool = null!;

   protected override async Task InitializeAsyncInternal()
   {
      _pool = _database.Container.Resolve<ISqliteConnectionPool>();
      _pool.ExecuteScalar("SELECT 1");
      await _database.DisposeAsync();
   }

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], OnlySqlitesPoolsAreDisposed)]
   public void it_refuses_a_command_with_ObjectDisposedException() => Invoking(() => _pool.ExecuteScalar("SELECT 1")).Must().Throw<ObjectDisposedException>();

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], OnlySqlitesPoolsAreDisposed)]
   public async Task it_refuses_an_asynchronous_command_with_ObjectDisposedException() =>
      await InvokingAsync(async () => await _pool.ExecuteNonQueryAsync("SELECT 1")).Must().ThrowAsync<ObjectDisposedException>();

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], OnlySqlitesPoolsAreDisposed)]
   public void it_refuses_to_give_its_connection_string_with_ObjectDisposedException() => Invoking(() => _pool.ConnectionString).Must().Throw<ObjectDisposedException>();
}
