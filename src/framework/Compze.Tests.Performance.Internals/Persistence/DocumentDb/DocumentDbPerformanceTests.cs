using Compze.DependencyInjection;
using Compze.Testing;
using Compze.Testing.Performance;
using Compze.Tests.Persistence.DocumentDb;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Compze.Tests.Performance.Internals.Persistence.DocumentDb;

[LongRunning]
class DocumentDbPerformanceTests([NotNull] string pluggableComponentsCombination) : DocumentDbTestsBase(pluggableComponentsCombination)
{
   [Test] public void Saves_100_documents_in_milliseconds_msSql_75_MySql_500_InMemory_8_PgSql_100_Orcl_100_DB2_300()
   {
      ServiceLocator.ExecuteInIsolatedScope(() =>
      {
         var updater = ServiceLocator.DocumentDbUpdater();

         //Warm up caches etc
         SaveOneNewUserInTransaction();

         //Performance: Fix the MySql opening connection slowness problem and up the number for MySql in this test
         //Performance: Look at why DB2 is so slow here.
         //Performance: See if using stored procedures and/or prepared statements speeds this up.
         TimeAsserter.Execute(
            action: SaveOneNewUserInTransaction,
            iterations: 100,
            maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 300, memory: 8, msSql: 75, mySql: 500, orcl: 100, pgSql: 75).Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.3)
         );
         return;

         void SaveOneNewUserInTransaction()
         {
            var user = new User();
            updater.Save(user);
         }
      });
   }
}