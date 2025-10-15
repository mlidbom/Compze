using System;
using Compze.Sql.DocumentDb.MicrosoftSql;
using Compze.Sql.DocumentDb.MySql;
using Compze.Sql.DocumentDb.PostgreSql;
using Compze.Sql.DocumentDb.Sqlite;
using Compze.Tessaging.Hosting.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Sql.MySql;
using Compze.Tessaging.Hosting.Sql.PostgreSql;
using Compze.Tessaging.Hosting.Sql.Sqlite;
using Compze.Tessaging.Sql.MicrosoftSql;
using Compze.Tessaging.Sql.MySql;
using Compze.Tessaging.Sql.PostgreSql;
using Compze.Tessaging.Sql.Sqlite;
using Compze.Tessaging.Teventive.EventStore.MicrosoftSql;
using Compze.Tessaging.Teventive.EventStore.MySql;
using Compze.Tessaging.Teventive.EventStore.PostgreSql;
using Compze.Tessaging.Teventive.EventStore.Sqlite;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class TestingSqlLayerRegistrar
{
   public static IDependencyRegistrar CurrentTestsConfiguredSqlLayer(this IDependencyRegistrar register) =>
      register.CurrentTestsConfiguredSqlLayer(Guid.NewGuid().ToString());

   public static IDependencyRegistrar CurrentTestsConfiguredSqlLayer(this IDependencyRegistrar register, string connectionStringName)
   {
      register.CurrentTestsDbPoolIfNotAlreadyRegistered();
      switch(TestEnv.SqlLayer)
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
         case SqlLayer.Sqlite:
            register.SqliteConnectionPool(connectionStringName)
                    .SqliteDocumentDb()
                    .SqliteEventStore()
                    .SqliteTessaging();
            break;
         case SqlLayer.SqliteMemory:
            register.SqliteMemoryConnectionPool(connectionStringName)
                    .SqliteDocumentDb()
                    .SqliteEventStore()
                    .SqliteTessaging();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }
}
