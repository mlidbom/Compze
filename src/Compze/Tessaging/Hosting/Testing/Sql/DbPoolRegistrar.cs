using System;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;
using Compze.Utilities.Testing.DbPool.MySql;
using Compze.Utilities.Testing.DbPool.PostgreSql;
using Compze.Utilities.Testing.DbPool.Sqlite;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this IComponentRegistrar register)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            register.MsSqlDbPoolIfNotAlreadyRegistered();
            break;
         case SqlLayer.MySql:
            register.MySqlDbPoolIfNotAlreadyRegistered();
            break;
         case SqlLayer.PostgreSql:
            register.PgSqlDbPoolIfNotAlreadyRegistered();
            break;
         case SqlLayer.Sqlite:
            register.SqliteDbPoolIfNotAlreadyRegistered();
            break;
         case SqlLayer.SqliteMemory:
            register.SqliteMemoryDbPoolIfNotAlreadyRegistered();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }
}
