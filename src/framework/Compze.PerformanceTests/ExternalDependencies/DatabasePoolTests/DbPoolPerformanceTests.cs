using System;
using Compze.DependencyInjection;
using Compze.Logging;
using Compze.Persistence.MySql.SystemExtensions;
using Compze.Persistence.MsSql.SystemExtensions;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.SystemCE;
using Compze.Testing;
using Compze.Testing.Performance;
using NUnit.Framework;

namespace Compze.Tests.ExternalDependencies.DatabasePoolTests;

public class DbPoolPerformanceTests : DbPoolTest
{
   [OneTimeSetUp]public void WarmUpCache()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      using var pool = CreatePool();
      pool.ConnectionStringFor(Guid.NewGuid().ToString());
   }

   [Test]
   public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_150_mySql_150_pgSql_150_orcl_300_db2_150()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

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
         maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 150, msSql: 150, mySql: 150, orcl: 300, pgSql: 150).Milliseconds());
   }

   [Test]
   public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_db2_50_msSql_75_mySql_75_orcl_100_pgSql_25()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      var maxTime = TestEnv.PersistenceLayer.ValueFor(db2: 50, msSql: 75, mySql: 75, orcl: 100, pgSql: 25).Milliseconds().EnvMultiply(instrumented:1.2);
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
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      var maxTotal = TestEnv.PersistenceLayer.ValueFor(db2: 100, msSql: 70, mySql: 175, orcl: 400, pgSql: 400).Milliseconds().EnvMultiply(instrumented:1.6);
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
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      TimeAsserter.Execute(
         action: () =>
         {
            using var dbPool = CreatePool();
            dbPool.SetLogLevel(LogLevel.Warning);
            dbPool.ConnectionStringFor(Guid.NewGuid().ToString());
         },
         iterations: 5,
         maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 100, msSql: 100, mySql: 170, orcl: 300, pgSql: 500).Milliseconds());
   }

   [Test]
   public void Repeated_fetching_of_same_connection_runs_20_times_in_1_milliseconds()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

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
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      var iterations = TestEnv.PersistenceLayer.ValueFor(db2: 50, msSql: 180, mySql: 24, orcl: 140, pgSql: 300);

      using var manager = CreatePool();
      manager.SetLogLevel(LogLevel.Warning);
      var reservationName = Guid.NewGuid().ToString();

      var useConnection = () => {};

      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            var msSqlConnectionProvider = IMsSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
            break;
         case PersistenceLayer.Memory:
            break;
         case PersistenceLayer.MySql:
            var mySqlConnectionProvider = IMySqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
            break;
         case PersistenceLayer.PostgreSql:
            var pgSqlConnectionProvider = IPgSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
            useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      useConnection();

      TimeAsserter.Execute(
         action: useConnection!,
         maxTotal: allowedTime,
         iterations : iterations
      );
   }

   public DbPoolPerformanceTests(string _) : base(_) {}
}