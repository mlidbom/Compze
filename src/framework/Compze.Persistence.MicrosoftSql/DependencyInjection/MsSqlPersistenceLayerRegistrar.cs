using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Configuration.Abstractions;
using Compze.DependencyInjection;
using Compze.DocumentDb.MicrosoftSql;
using Compze.EventStore.MicrosoftSql;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Persistence.MicrosoftSql.Testing;
using Compze.Tessaging.Buses;
using Compze.Tessaging.MicrosoftSql;

namespace Compze.Persistence.MicrosoftSql.DependencyInjection;

public static class MsSqlPersistenceLayerRegistrar
{
   public static void RegisterMsSqlPersistenceLayer(this IEndpointBuilder @this) =>
      @this.Container.RegisterMsSqlPersistenceLayer(@this.Configuration.ConnectionStringName);

   //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
   public static void RegisterMsSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
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


      //Service bus
      container.RegisterMsSqlTessaging();

      //DocumentDB
      container.RegisterMsSqlDocumentDb();

      //Event store
      container.RegisterMsSqlEventStore();
   }
}
