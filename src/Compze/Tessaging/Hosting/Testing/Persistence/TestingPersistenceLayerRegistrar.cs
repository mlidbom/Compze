using System;
using Compze.Persistence.DocumentDb.MicrosoftSql;
using Compze.Persistence.DocumentDb.MySql;
using Compze.Persistence.DocumentDb.PostgreSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Persistence.MicrosoftSql;
using Compze.Tessaging.Hosting.Persistence.MySql;
using Compze.Tessaging.Hosting.Persistence.PostgreSql;
using Compze.Tessaging.Persistence.InMemory.DependencyInjection;
using Compze.Tessaging.Persistence.MicrosoftSql;
using Compze.Tessaging.Persistence.MySql;
using Compze.Tessaging.Persistence.PostgreSql;
using Compze.Tessaging.Teventive.EventStore.MicrosoftSql;
using Compze.Tessaging.Teventive.EventStore.MySql;
using Compze.Tessaging.Teventive.EventStore.PostgreSql;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Persistence;

public static class TestingPersistenceLayerRegistrar
{
   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this)
      => @this.Container.Register().CurrentTestsConfiguredPersistenceLayer(@this.Configuration.ConnectionStringName);

   public static IDependencyRegistrar NewDbPoolPersistenceLayer(this IDependencyRegistrar register)
   {
      register.CurrentTestsConfiguredPersistenceLayer(Guid.NewGuid().ToString());
      return register;
   }

   public static IDependencyRegistrar CurrentTestsConfiguredPersistenceLayer(this IDependencyRegistrar register, string connectionStringName)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            register.MsSqlConnectionPool(connectionStringName);
            register.MsSqlDocumentDb();
            register.MsSqlEventStore();
            register.MsSqlTessaging();
            break;
         case PersistenceLayer.MySql:
            register.MySqlConnectionPool(connectionStringName);
            register.MySqlDocumentDb();
            register.MySqlEventStore();
            register.MySqlTessaging();
            break;
         case PersistenceLayer.PostgreSql:
            register.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
            register.PgSqlDocumentDb();
            register.PgSqlEventStore();
            register.RegisterPgSqlTessaging();
            break;
         case PersistenceLayer.Memory:
            register.InMemoryPersistenceLayer();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }

   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            container.RegisterMsSqlConnectionPool(connectionStringName);
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
            container.RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
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
