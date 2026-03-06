using Compze.Abstractions.Configuration.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

public static class SqliteConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.SqliteProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   // ReSharper disable once UnusedMethodReturnValue.Local
   static IComponentRegistrar SqliteProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName))));
}
