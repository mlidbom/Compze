using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Sql.MicrosoftSql;

public static class MsSqlSqlLayerRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar MsSqlConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
            return testingRegistrar.Register(connectionStringName);
      }
      if(registrar.RunMode.IsTesting)
      {
         registrar.MicrosoftSqlDbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
      } else
      {
         registrar.MsSqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IComponentRegistrar MsSqlProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());


}
