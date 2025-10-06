using System;
using Compze.Persistence.DocumentDb.MicrosoftSql;
using Compze.Persistence.DocumentDb.MySql;
using Compze.Persistence.DocumentDb.PostgreSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Persistence.MicrosoftSql;
using Compze.Tessaging.Hosting.Persistence.MySql;
using Compze.Tessaging.Persistence.InMemory.DependencyInjection;
using Compze.Tessaging.Persistence.MicrosoftSql;
using Compze.Tessaging.Persistence.MySql;
using Compze.Tessaging.Persistence.PostgreSql;
using Compze.Tessaging.Teventive.EventStore.MicrosoftSql;
using Compze.Tessaging.Teventive.EventStore.MySql;
using Compze.Tessaging.Teventive.EventStore.PostgreSql;
using Compze.Utilities.DependencyInjection;

namespace Compze.Tessaging.Hosting.Testing.Persistence;

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