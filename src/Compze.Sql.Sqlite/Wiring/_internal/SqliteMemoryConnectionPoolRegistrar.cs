using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring._internal;

static class SqliteMemoryConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string databaseName);
   }

   public static IComponentRegistrar SqliteMemoryConnectionPool(this IComponentRegistrar registrar, string databaseName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(databaseName);
      } else
      {
         //todo:review: Should a production composition be able to declare an in-memory sqlite domain database as transient storage?
         //Supporting it needs more than this pool: a keep-alive connection so the database survives between uses, and an
         //in-memory route for the type-id interner's own database, whose production wiring resolves connection strings from configuration.
         throw new InvalidOperationException("An in-memory sqlite domain database is composable only under the testing hosts for now.");
      }
   }
}
