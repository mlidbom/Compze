using System;
using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Integration.Internals.Testing.Sql;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Wiring;
using NUnit.Framework;

namespace Compze.Tests.Performance.Internals.ExternalDependencies.DatabasePoolTests;

public class DbPoolPerformanceTests(string pluggableComponentsCombination) : DbPoolTest(pluggableComponentsCombination)
{
   [OneTimeSetUp]public void WarmUpCache()
   {
      using var pool = CreatePool();
      pool.ConnectionStringFor(Guid.NewGuid().ToString());
   }

   [Test]
   public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_150_mySql_150_pgSql_150_orcl_300_db2_150()
   {
      var dbName = Guid.NewGuid().ToString();

      TimeAsserter.Execute(
         action:
         () =>
         {
            using var dbPool = CreatePool();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 150, mySql: 150, pgSql: 150, sqlite: 150).Milliseconds());
   }

   [Test]
   public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_db2_50_msSql_75_mySql_75_orcl_100_pgSql_25()
   {
      var maxTime = TestEnv.SqlLayer.ValueFor(msSql: 75, mySql: 75, pgSql: 25, sqlite: 75).Milliseconds().EnvMultiply(instrumented:1.2);
      var dbName = Guid.NewGuid().ToString();
      TimeAsserter.ExecuteThreaded(
         action:
         () =>
         {
            using var dbPool = CreatePool();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: maxTime);
   }

   [Test]
   public void Multiple_threads_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_125_mySql_175_pgSql_400_orcl_400_db2_100()
   {
      var maxTotal = TestEnv.SqlLayer.ValueFor(msSql: 70, mySql: 175, pgSql: 400, sqlite: 125).Milliseconds().EnvMultiply(instrumented:1.6);
      TimeAsserter.ExecuteThreaded(
         action: () =>
         {
            using var dbPool = CreatePool();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: maxTotal);
   }

   [Test]
   public void Single_thread_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_100_mySql_100_pgSql_500_orcl_300_db2_100()
   {
      TimeAsserter.Execute(
         action: () =>
         {
            using var dbPool = CreatePool();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 100, mySql: 170, pgSql: 500, sqlite: 100).Milliseconds());
   }

   [Test]
   public void Repeated_fetching_of_same_connection_runs_20_times_in_1_milliseconds()
   {
      var dbName = Guid.NewGuid().ToString();
      using var manager = CreatePool();
      manager.SetLogLevel(LogLevel.Warning);
      manager.ConnectionStringFor(dbName);

      TimeAsserter.Execute(
         action: () => manager.ConnectionStringFor(dbName),
         iterations: 20,
         maxTotal: 1.Milliseconds());
   }

   [Test]
   public void Once_DB_Fetched_Can_use_XX_connections_in_10_millisecond_db2_50_MsSql_180_MySql_24_Oracle_140_PgSql_300()
   {
      var allowedTime = 10.Milliseconds().EnvMultiply(instrumented:2);
      var iterations = TestEnv.SqlLayer.ValueFor(msSql: 180, mySql: 24, pgSql: 300, sqlite: 180);

      using var manager = CreatePool();
      manager.SetLogLevel(LogLevel.Warning);
      var reservationName = Guid.NewGuid().ToString();

      Action useConnection;

      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            var msSqlConnectionProvider = IMsSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.MySql:
            var mySqlConnectionProvider = IMySqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.PostgreSql:
            var pgSqlConnectionProvider = IPgSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.Sqlite:
         case SqlLayer.SqliteMemory:
            var sqliteConnectionProvider = ISqliteConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => sqliteConnectionProvider.UseConnection(_ => {});
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      useConnection();

      TimeAsserter.Execute(
         action: useConnection,
         maxTotal: allowedTime,
         iterations : iterations
      );
   }
}