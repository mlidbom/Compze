using Compze.Common.Configuration;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.Sqlite;

namespace Compze.Tessaging.Hosting.Sql.Sqlite;

public static class SqliteSqlLayerRegistrar
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

   static IComponentRegistrar SqliteProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());
}
