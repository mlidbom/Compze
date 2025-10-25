using System;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Sql.DocumentDb.MicrosoftSql.Wiring;
using Compze.Sql.DocumentDb.MySql.Wiring;
using Compze.Sql.DocumentDb.PostgreSql.Wiring;
using Compze.Sql.DocumentDb.Sqlite.Wiring;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.PostgreSql;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Sql.MicrosoftSql;
using Compze.Tessaging.Sql.MySql;
using Compze.Tessaging.Sql.PostgreSql;
using Compze.Tessaging.Sql.Sqlite;
using Compze.Tessaging.Teventive.TeventStore.MicrosoftSql;
using Compze.Tessaging.Teventive.TeventStore.MySql;
using Compze.Tessaging.Teventive.TeventStore.PostgreSql;
using Compze.Tessaging.Teventive.TeventStore.Sqlite;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class TestingSqlLayerRegistrar
{
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register) =>
      register.CurrentTestsConfiguredSqlLayer(Guid.NewGuid().ToString());

   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            register.MsSqlConnectionPool(connectionStringName)
                    .MsSqlDocumentDb()
                    .MsSqlTeventStore()
                    .MsSqlTessaging();
            break;
         case SqlLayer.MySql:
            register.MySqlConnectionPool(connectionStringName)
                    .MySqlDocumentDb()
                    .MySqlTeventStore()
                    .MySqlTessaging();
            break;
         case SqlLayer.PostgreSql:
            register.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                    .PgSqlDocumentDb()
                    .PgSqlTeventStore()
                    .PgSqlTessaging();
            break;
         case SqlLayer.Sqlite:
            register.SqliteConnectionPool(connectionStringName)
                    .SqliteDocumentDb()
                    .SqliteTeventStore()
                    .SqliteTessaging();
            break;
         case SqlLayer.SqliteMemory:
            register.SqliteMemoryConnectionPool(connectionStringName)
                    .SqliteDocumentDb()
                    .SqliteTeventStore()
                    .SqliteTessaging();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }
}
