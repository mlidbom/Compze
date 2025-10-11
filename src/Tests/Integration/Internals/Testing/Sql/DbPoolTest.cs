using System;
using Compze.Sql.Common.Abstractions;
using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.DbPool;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;
using Compze.Utilities.Testing.DbPool.MySql;
using Compze.Utilities.Testing.DbPool.PostgreSql;
using Compze.Wiring;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Integration.Internals.Testing.Sql;

public abstract class DbPoolTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   public static DbPool CreatePool() =>
      TestEnv.SqlLayer.Current switch
      {
         SqlLayer.MicrosoftSqlServer => new MsSqlDbPool(),
         SqlLayer.MySql => new MySqlDbPool(),
         SqlLayer.PostgreSql => new PgSqlDbPool(),
         _ => throw new ArgumentOutOfRangeException()
      };

   internal static void UseConnection(string connectionString, DbPool pool, Action<ICompzeDbConnection> func)
   {
      switch(TestEnv.SqlLayer.Current)
      {
         case SqlLayer.MicrosoftSqlServer:
            UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case SqlLayer.PostgreSql:
            UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case SqlLayer.MySql:
            UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   static void UseMySqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IMySqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UsePgSqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IPgSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UseMsSqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IMsSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);
}