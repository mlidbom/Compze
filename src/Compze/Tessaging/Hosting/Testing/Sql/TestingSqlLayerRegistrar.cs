using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MicrosoftSql.DocumentDb.Wiring;
using Compze.Sql.MicrosoftSql.Tessaging;
using Compze.Sql.MicrosoftSql.TEventStore;
using Compze.Sql.MySql.DocumentDb.Wiring;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.MySql.Tessaging;
using Compze.Sql.MySql.TEventStore;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.DocumentDb.Wiring;
using Compze.Sql.PostgreSql.Tessaging;
using Compze.Sql.PostgreSql.TEventStore;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.DocumentDb.Wiring;
using Compze.Sql.Sqlite.Tessaging;
using Compze.Sql.Sqlite.TEventStore;
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
