using System;
using Compze.Sql.DocumentDb.MicrosoftSql;
using Compze.Sql.DocumentDb.MySql;
using Compze.Sql.DocumentDb.PostgreSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Sql.MySql;
using Compze.Tessaging.Hosting.Sql.PostgreSql;
using Compze.Tessaging.Sql.MicrosoftSql;
using Compze.Tessaging.Sql.MySql;
using Compze.Tessaging.Sql.PostgreSql;
using Compze.Tessaging.Teventive.EventStore.MicrosoftSql;
using Compze.Tessaging.Teventive.EventStore.MySql;
using Compze.Tessaging.Teventive.EventStore.PostgreSql;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class TestingSqlLayerRegistrar
{
   public static void RegisterCurrentTestsConfiguredSqlLayer(this IEndpointBuilder @this)
      => @this.Container.Register().CurrentTestsConfiguredSqlLayer(@this.Configuration.ConnectionStringName);

   public static IDependencyRegistrar NewDbPoolSqlLayer(this IDependencyRegistrar register)
   {
      register.CurrentTestsConfiguredSqlLayer(Guid.NewGuid().ToString());
      return register;
   }

   public static IDependencyRegistrar CurrentTestsConfiguredSqlLayer(this IDependencyRegistrar register) =>
      register.CurrentTestsConfiguredSqlLayer(Guid.NewGuid().ToString());

   public static IDependencyRegistrar CurrentTestsConfiguredSqlLayer(this IDependencyRegistrar register, string connectionStringName)
   {
      switch(TestEnv.SqlLayer.Current)
      {
         case SqlLayer.MicrosoftSqlServer:
            register.MsSqlConnectionPool(connectionStringName)
                    .MsSqlDocumentDb()
                    .MsSqlEventStore()
                    .MsSqlTessaging();
            break;
         case SqlLayer.MySql:
            register.MySqlConnectionPool(connectionStringName)
                    .MySqlDocumentDb()
                    .MySqlEventStore()
                    .MySqlTessaging();
            break;
         case SqlLayer.PostgreSql:
            register.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                    .PgSqlDocumentDb()
                    .PgSqlEventStore()
                    .PgSqlTessaging();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }
}
