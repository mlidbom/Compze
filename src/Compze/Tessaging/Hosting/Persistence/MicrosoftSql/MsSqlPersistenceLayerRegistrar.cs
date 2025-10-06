using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Persistence.MicrosoftSql;

public static class MsSqlPersistenceLayerRegistrar
{
   internal static void RegisterMsSqlConnectionPoolIfNotAlreadyRegistered(this IEndpointBuilder @this) =>
      @this.Container.RegisterMsSqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static void RegisterMsSqlConnectionPoolIfNotAlreadyRegistered(this IDependencyInjectionContainer container, string connectionStringName)
   {
      if(container.IsRegistered<IMsSqlConnectionPool>()) return;

      //Connection management
      if(container.RunMode.IsTesting)
      {
         container.Register(Singleton.For<MsSqlDbPool>()
                                     .CreatedBy((IConfigurationParameterProvider _) => new MsSqlDbPool())
                                     .DelegateToParentServiceLocatorWhenCloning());

         container.Register(
            Singleton.For<IMsSqlConnectionPool>()
                     .CreatedBy((MsSqlDbPool pool) => IMsSqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
         );
      } else
      {
         container.Register(
            Singleton.For<IMsSqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                     .DelegateToParentServiceLocatorWhenCloning());
      }
   }
}
