using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Persistence.MicrosoftSql;

public static class MsSqlPersistenceLayerRegistrar
{
   internal static void RegisterMsSqlConnectionPool(this IEndpointBuilder @this) =>
      @this.Container.RegisterMsSqlConnectionPool(@this.Configuration.ConnectionStringName);

   public static IDependencyRegistrar MsSqlConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.Container().RegisterMsSqlConnectionPool(connectionStringName);
      return registrar;
   }

   public static void RegisterMsSqlConnectionPool(this IDependencyInjectionContainer container, string connectionStringName)
   {
      if(container.RunMode.IsTesting)
      {
         container.Register().DbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
      } else
      {
         container.Register().MsSqlProductionConnectionPool(connectionStringName);
      }
   }

   public static IDependencyRegistrar DbPoolAndConnectionPoolForRandomConnectionString(this IDependencyRegistrar registrar)
      => registrar.DbPoolAndConnectionPoolForConnectionStringName(Guid.NewGuid().ToString());

   public static IDependencyRegistrar MsSqlProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   public static IDependencyRegistrar DbPoolAndConnectionPoolForConnectionStringName(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.Register(Singleton.For<MsSqlDbPool>()
                                  .CreatedBy((IConfigurationParameterProvider _) => new MsSqlDbPool())
                                  .DelegateToParentServiceLocatorWhenCloning());

      return registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((MsSqlDbPool pool) => IMsSqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}
