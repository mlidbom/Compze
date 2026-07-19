using Compze.Abstractions.Configuration.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.Internals.Sql.MySql.Wiring;

public static class MySqlConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar MySqlConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.MySqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IComponentRegistrar MySqlProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName))));
   }
}
