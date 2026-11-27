using System.Transactions;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Sql.Sqlite._internal;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Sql.InternalSpecifications.ProbeCheck;

//PROBE CHECK, experiment only: a nested use and two concurrent children, each of which the probe must record.
public class The_reentry_probe_sees_reentries : UniversalTestBase
{
   readonly DomainDatabaseOnTheCurrentSqlLayer _database = DomainDatabaseOnTheCurrentSqlLayer.Named("ReentryProbeCheck");
   protected override async Task DisposeAsyncInternal() => await _database.DisposeAsync();

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql, SqlLayer.Sqlite], "check once")]
   public void a_nested_synchronous_use()
   {
      var pool = _database.Container.Resolve<ISqliteConnectionPool>();
      using var scope = new TransactionScope();
      pool.UseConnection(_ => pool.ExecuteScalar("SELECT 1 -- nested synchronous use"));
      scope.Complete();
   }

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql, SqlLayer.Sqlite], "check once")]
   public async Task two_concurrent_children()
   {
      var pool = _database.Container.Resolve<ISqliteConnectionPool>();
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      await pool.UseConnectionAsync(async _ =>
      {
         await Task.WhenAll(pool.ExecuteNonQueryAsync("SELECT 1 -- first child"), pool.ExecuteNonQueryAsync("SELECT 1 -- second child"));
         return 0;
      });
      scope.Complete();
   }
}
