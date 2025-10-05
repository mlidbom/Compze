using Compze.Configuration.Abstractions;
using Compze.DependencyInjection;
using Compze.DocumentDb.MySql;
using Compze.EventStore.MySql;
using Compze.Persistence.MySql.Infrastructure;
using Compze.Persistence.MySql.Testing;
using Compze.Tessaging.Buses;
using Compze.Tessaging.MySql;

namespace Compze.Persistence.MySql.DependencyInjection;

public static class MySqlPersistenceLayerRegistrar
{
   public static void RegisterMySqlPersistenceLayer(this IEndpointBuilder @this) =>
      @this.Container.RegisterMySqlPersistenceLayer(@this.Configuration.ConnectionStringName);

   public static void RegisterMySqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
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

      //Service bus
      container.RegisterMySqlTessaging();

      //DocumentDB
      container.RegisterMySqlDocumentDb();

      //Event store
      container.RegisterMySqlEventStore();
   }
}