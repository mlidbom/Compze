using System;
using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.Persistence.InMemory.DependencyInjection;
using Compze.Persistence.MsSql.DependencyInjection;
using Compze.Persistence.MySql.DependencyInjection;
using Compze.Persistence.PgSql.DependencyInjection;

namespace Compze.Testing.Persistence;

public static class TestingPersistenceLayerRegistrar
{
   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IEndpointBuilder @this) => RegisterCurrentTestsConfiguredPersistenceLayer(@this.Container, @this.Configuration.ConnectionStringName);

   public static void RegisterCurrentTestsConfiguredPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            container.RegisterMsSqlPersistenceLayer(connectionStringName);
            break;
         case PersistenceLayer.MySql:
            container.RegisterMySqlPersistenceLayer(connectionStringName);
            break;
         case PersistenceLayer.PostgreSql:
            container.RegisterPgSqlPersistenceLayer(connectionStringName);
            break;
         case PersistenceLayer.Memory:
            container.RegisterInMemoryPersistenceLayer(connectionStringName);
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}