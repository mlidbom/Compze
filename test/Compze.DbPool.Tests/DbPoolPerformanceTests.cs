using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Internals.Logging;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.DbPool.Tests;

public class DbPoolPerformanceTests : DbPoolTestBase
{
   const string SkipReason = "Runnnig DbPool performance tests along with other tests is death to test performance. These tests should only run in total isolation";

   public DbPoolPerformanceTests()
   {
      using var pool = ResolvePool();//warmup
      pool.ConnectionStringFor(Guid.NewGuid().ToString());
   }

   [PCT(Skip = SkipReason)] public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds()
   {
      var dbName = Guid.NewGuid().ToString();

      TimeAsserter.Execute(
         action:
         () =>
         {
            using var container = CreateContainer();
            using var dbPool = container.Resolve<DbPool>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 200, mySql: 200, pgSql: 200, sqlite: 200, sqliteMemory: 200).Milliseconds());
   }

   [PCT(Skip = SkipReason)] public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_XX_milliseconds()
   {
      var maxTime = TestEnv.SqlLayer.ValueFor(msSql: 100, mySql: 200, pgSql: 35, sqlite: 150, sqliteMemory: 150).Milliseconds().EnvMultiply(instrumented: 1.2);
      var dbName = Guid.NewGuid().ToString();
      TimeAsserter.ExecuteThreaded(
         action:
         () =>
         {
            using var container = CreateContainer();
            using var dbPool = container.Resolve<DbPool>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(dbName);
         },
         iterations: 5,
         maxTotal: maxTime);
   }

   [PCT(Skip = SkipReason)] public void Multiple_threads_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_125_mySql_175_pgSql_400_orcl_400_db2_100()
   {
      var maxTotal = TestEnv.SqlLayer.ValueFor(msSql: 70, mySql: 400, pgSql: 400, sqlite: 125, sqliteMemory: 125).Milliseconds().EnvMultiply(instrumented: 1.6);
      TimeAsserter.ExecuteThreaded(
         action: () =>
         {
            using var container = CreateContainer();
            using var dbPool = container.Resolve<DbPool>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: maxTotal);
   }

   [PCT(Skip = SkipReason)] public void Single_thread_can_reserve_and_release_5_differently_named_databases_in_XX_milliseconds()
   {
      TimeAsserter.Execute(
         action: () =>
         {
            using var container = CreateContainer();
            using var dbPool = container.Resolve<DbPool>();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 140, mySql: 250, pgSql: 500, sqlite: 200, sqliteMemory: 200).Milliseconds());
   }

   [PCT(Skip = SkipReason)] public void Repeated_fetching_of_same_connection_runs_20_times_in_1_milliseconds()
   {
      var dbName = Guid.NewGuid().ToString();
      using var container = CreateContainer();
      using var pool = container.Resolve<DbPool>();
      pool.SetLogLevel(LogLevel.Warning);
      pool.ConnectionStringFor(dbName);

      TimeAsserter.Execute(
         action: () => pool.ConnectionStringFor(dbName),
         iterations: 20,
         maxTotal: 1.Milliseconds());
   }

   [PCT(Skip = SkipReason)] public void Once_DB_Fetched_Can_use_XX_connections_in_10_millisecond_db2_50_MsSql_180_MySql_24_Oracle_140_PgSql_300()
   {
      var allowedTime = 10.Milliseconds().EnvMultiply(instrumented: 2);
      var iterations = TestEnv.SqlLayer.ValueFor(msSql: 180, mySql: 24, pgSql: 300, sqlite: 180, sqliteMemory: 180);

      using var container = CreateContainer();
      using var pool = container.Resolve<DbPool>();
      pool.SetLogLevel(LogLevel.Warning);
      var reservationName = Guid.NewGuid().ToString();

      var connectionString = pool.ConnectionStringFor(reservationName);
      Action useConnection = () =>
      {
         using var connection = CreateConnection(connectionString);
         connection.Open();
      };

      useConnection();

      TimeAsserter.Execute(
         action: useConnection,
         maxTotal: allowedTime,
         iterations: iterations
      );
   }
}

