using System;
using Compze.Sql.Common.Abstractions;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.PostgreSql;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.DbPool;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;
using Compze.Utilities.Testing.DbPool.MySql;
using Compze.Utilities.Testing.DbPool.PostgreSql;
using Compze.Utilities.Testing.DbPool.Sqlite;
using Compze.Wiring;

namespace Compze.Tests.Common.Testing.Sql;

public abstract class DbPoolTestBase : UniversalTestBase, IDisposable
{
   protected readonly DbPoolBase Pool = CreatePool();
   public void Dispose() => Pool.Dispose();

   protected static DbPoolBase CreatePool() =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MicrosoftSqlServer => new MsSqlDbPool(),
         SqlLayer.MySql              => new MySqlDbPool(),
         SqlLayer.PostgreSql         => new PgSqlDbPool(),
         SqlLayer.Sqlite             => new SqliteDbPool(),
         SqlLayer.SqliteMemory       => new SqliteMemoryDbPool(),
         _                           => throw new ArgumentOutOfRangeException()
      };

   internal static void UseConnection(string connectionString, DbPoolBase pool, Action<ICompzeDbConnection> func)
   {
      switch(TestEnv.SqlLayer)
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
         case SqlLayer.Sqlite:
         case SqlLayer.SqliteMemory:
            UseSqliteConnection(pool.ConnectionStringFor(connectionString), func);
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

   static void UseSqliteConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      ISqliteConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);
}
