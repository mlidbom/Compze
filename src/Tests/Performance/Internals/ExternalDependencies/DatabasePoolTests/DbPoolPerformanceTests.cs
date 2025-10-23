using System;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.PostgreSql;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Common.Testing.Sql;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.DbPool;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tests.Performance.Internals.ExternalDependencies.DatabasePoolTests;

public class DbPoolPerformanceTests : DbPoolTestBase
{
   public DbPoolPerformanceTests()
   {
      using var pool = ResolvePool();//warmup
      pool.ConnectionStringFor(Guid.NewGuid().ToString());
   }

   [PCT] public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_150_mySql_150_pgSql_150_orcl_300_db2_150()
   {
      var dbName = Guid.NewGuid().ToString();

      TimeAsserter.Execute(
         action:
         () =>
         {
            using var serviceLocator = CreateServiceLocator();
            using var dbPool = serviceLocator.Resolve<DbPoolBase>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 150, mySql: 150, pgSql: 150, sqlite: 150, sqliteMemory: 150).Milliseconds());
   }

   [PCT] public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_db2_50_msSql_75_mySql_75_orcl_100_pgSql_25()
   {
      var maxTime = TestEnv.SqlLayer.ValueFor(msSql: 75, mySql: 75, pgSql: 25, sqlite: 75, sqliteMemory: 75).Milliseconds().EnvMultiply(instrumented: 1.2);
      var dbName = Guid.NewGuid().ToString();
      TimeAsserter.ExecuteThreaded(
         action:
         () =>
         {
            using var serviceLocator = CreateServiceLocator();
            using var dbPool = serviceLocator.Resolve<DbPoolBase>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: maxTime);
   }

   [PCT] public void Multiple_threads_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_125_mySql_175_pgSql_400_orcl_400_db2_100()
   {
      var maxTotal = TestEnv.SqlLayer.ValueFor(msSql: 70, mySql: 175, pgSql: 400, sqlite: 125, sqliteMemory: 125).Milliseconds().EnvMultiply(instrumented: 1.6);
      TimeAsserter.ExecuteThreaded(
         action: () =>
         {
            using var serviceLocator = CreateServiceLocator();
            using var dbPool = serviceLocator.Resolve<DbPoolBase>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: maxTotal);
   }

   [PCT] public void Single_thread_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_100_mySql_100_pgSql_500_orcl_300_db2_100()
   {
      TimeAsserter.Execute(
         action: () =>
         {
            using var serviceLocator = CreateServiceLocator();
            using var dbPool = serviceLocator.Resolve<DbPoolBase>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 100, mySql: 170, pgSql: 500, sqlite: 100, sqliteMemory: 100).Milliseconds());
   }

   [PCT] public void Repeated_fetching_of_same_connection_runs_20_times_in_1_milliseconds()
   {
      var dbName = Guid.NewGuid().ToString();
      using var serviceLocator = CreateServiceLocator();
      using var pool = serviceLocator.Resolve<DbPoolBase>();
      pool.SetLogLevel(LogLevel.Warning);
      pool.ConnectionStringFor(dbName);

      TimeAsserter.Execute(
         action: () => pool.ConnectionStringFor(dbName),
         iterations: 20,
         maxTotal: 1.Milliseconds());
   }

   [PCT] public void Once_DB_Fetched_Can_use_XX_connections_in_10_millisecond_db2_50_MsSql_180_MySql_24_Oracle_140_PgSql_300()
   {
      var allowedTime = 10.Milliseconds().EnvMultiply(instrumented: 2);
      var iterations = TestEnv.SqlLayer.ValueFor(msSql: 180, mySql: 24, pgSql: 300, sqlite: 180, sqliteMemory: 180);

      using var serviceLocator = CreateServiceLocator();
      using var pool = serviceLocator.Resolve<DbPoolBase>();
      pool.SetLogLevel(LogLevel.Warning);
      var reservationName = Guid.NewGuid().ToString();

      Action useConnection;

      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            var msSqlConnectionProvider = IMsSqlConnectionPool.CreateInstance(pool.ConnectionStringFor(reservationName));
            useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.MySql:
            var mySqlConnectionProvider = IMySqlConnectionPool.CreateInstance(pool.ConnectionStringFor(reservationName));
            useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.PostgreSql:
            var pgSqlConnectionProvider = IPgSqlConnectionPool.CreateInstance(pool.ConnectionStringFor(reservationName));
            useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
            break;
         case SqlLayer.Sqlite:
         case SqlLayer.SqliteMemory:
            var sqliteConnectionProvider = ISqliteConnectionPool.CreateInstance(pool.ConnectionStringFor(reservationName));
            useConnection = () => sqliteConnectionProvider.UseConnection(_ => {});
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      useConnection();

      TimeAsserter.Execute(
         action: useConnection,
         maxTotal: allowedTime,
         iterations: iterations
      );
   }
}
