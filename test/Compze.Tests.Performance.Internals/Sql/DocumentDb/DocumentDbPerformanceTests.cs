using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE;


namespace Compze.Tests.Performance.Internals.Sql.DocumentDb;

[LongRunning]
public class DocumentDbPerformanceTests : DocumentDbTestsBase
{
   [PCT] public void Saves_XX_documents_in_100_milliseconds()
   {
      ServiceLocator.ExecuteInIsolatedScope(() =>
      {
         var updater = ServiceLocator.DocumentDbUpdater();

         //Warm up caches etc
         SaveOneNewUserInTransaction();

         TimeAsserter.Execute(
            action: SaveOneNewUserInTransaction,
            iterations: TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 8, pgSql: 8, sqlite: 4, sqliteMemory: 6)
                               .EnvDivide(instrumented:2.2, unoptimized:1.3),
            maxTotal: 100.Milliseconds()
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