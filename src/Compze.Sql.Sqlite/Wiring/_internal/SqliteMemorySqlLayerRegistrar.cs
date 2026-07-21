using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;

namespace Compze.Sql.Sqlite.Wiring._internal;

static class SqliteMemoryConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteMemoryConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         //tod: Why? Any reason not to support using Sqlite memory as for transient storage in production?
         throw new InvalidOperationException("SqliteMemory is only supported in testing mode");
      }
   }
}
