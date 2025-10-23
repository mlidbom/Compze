using System;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;
using Compze.Utilities.Testing.DbPool.MySql;
using Compze.Utilities.Testing.DbPool.PostgreSql;
using Compze.Utilities.Testing.DbPool.Sqlite;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this IComponentRegistrar register)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            register.MsSqlDbPoolSqlLayerIfNotAlreadyRegistered();
            break;
         case SqlLayer.MySql:
            register.MySqlDbPoolSqlLayerIfNotAlreadyRegistered();
            break;
         case SqlLayer.PostgreSql:
            register.PgSqlDbPoolSqlLayerIfNotAlreadyRegistered();
            break;
         case SqlLayer.Sqlite:
            register.SqliteDbPoolSqlLayerIfNotAlreadyRegistered();
            break;
         case SqlLayer.SqliteMemory:
            register.SqliteMemoryDbPoolSqlLayerIfNotAlreadyRegistered();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return register;
   }
}
