using System;
using Compze.DependencyInjection;
using Compze.DocumentDb.MicrosoftSql;
using Compze.DocumentDb.MySql;
using Compze.DocumentDb.PostgreSql;
using Compze.EventStore.MicrosoftSql;
using Compze.EventStore.MySql;
using Compze.EventStore.PostgreSql;
using Compze.Persistence.InMemory.DependencyInjection;
using Compze.Persistence.MicrosoftSql.DependencyInjection;
using Compze.Persistence.MySql.DependencyInjection;
using Compze.Persistence.PostgreSql.DependencyInjection;
using Compze.Tessaging.Buses;
using Compze.Tessaging.MicrosoftSql;
using Compze.Tessaging.MySql;
using Compze.Tessaging.PostgreSql;

namespace Compze.Testing.Persistence;

public static class TestingPersistenceLayerRegistrar
{
   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this) => RegisterCurrentTestsConfiguredPersistenceLayer(@this.Container, @this.Configuration.ConnectionStringName);

   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            container.RegisterMsSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
            container.RegisterMsSqlDocumentDb();
            container.RegisterMsSqlEventStore();
            container.RegisterMsSqlTessaging();
            break;
         case PersistenceLayer.MySql:
            container.RegisterMySqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
            container.RegisterMySqlDocumentDb();
            container.RegisterMySqlEventStore();
            container.RegisterMySqlTessaging();
            break;
         case PersistenceLayer.PostgreSql:
            container.RegisterPgSqlDocumentDb();
            container.RegisterPgSqlEventStore();
            container.RegisterPgSqlTessaging();
            break;
         case PersistenceLayer.Memory:
            container.RegisterInMemoryPersistenceLayer(connectionStringName);
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}