using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.DependencyInjection;
using Compze.Wiring;
using FluentAssertions.Extensions;
using Xunit;

namespace Compze.Tests.Performance.Internals.XUnit.Sql.DocumentDb;

[Performance, LongRunning]
public class DocumentDbPerformanceTests : DocumentDbTestsBase
{
   [PCT] public void Saves_100_documents_in_milliseconds_msSql_75_MySql_500_InMemory_8_PgSql_100_Orcl_100_DB2_300()
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
            maxTotal: TestEnv.SqlLayer.ValueFor(msSql: 75, mySql: 500, pgSql: 75, sqlite: 400).Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.3)
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