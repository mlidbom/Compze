using Compze.Configuration.Abstractions;
using Compze.DependencyInjection;
using Compze.Persistence.MySql.Infrastructure;
using Compze.Tessaging.Hosting;
using Compze.Testing.DbPool.MySql;

namespace Compze.Persistence.MySql.DependencyInjection;

public static class MySqlPersistenceLayerRegistrar
{
   internal static void RegisterMySqlConnectionPoolIfNotAlreadyRegistered(this IEndpointBuilder @this) =>
      @this.Container.RegisterMySqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static void RegisterMySqlConnectionPoolIfNotAlreadyRegistered(this IDependencyInjectionContainer container, string connectionStringName)
   {
      if(container.IsRegistered<IMySqlConnectionPool>()) return;

      //Connection management
      if(container.RunMode.IsTesting)
      {
         container.Register(Singleton.For<MySqlDbPool>()
                                     .CreatedBy((IConfigurationParameterProvider _) => new MySqlDbPool())
                                     .DelegateToParentServiceLocatorWhenCloning());

         container.Register(
            Singleton.For<IMySqlConnectionPool>()
                     .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
         );
      } else
      {
         container.Register(
            Singleton.For<IMySqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                     .DelegateToParentServiceLocatorWhenCloning());
      }
   }
}