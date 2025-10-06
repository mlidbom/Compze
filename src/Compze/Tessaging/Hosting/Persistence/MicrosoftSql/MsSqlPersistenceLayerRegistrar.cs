using Compze.Configuration;
using Compze.DependencyInjection;
using Compze.Hosting.Abstractions;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Testing.DbPool.MicrosoftSql;

namespace Compze.Persistence.MicrosoftSql;

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
