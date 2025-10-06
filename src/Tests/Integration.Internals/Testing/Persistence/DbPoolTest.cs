using System;
using Compze.DependencyInjection;
using Compze.Persistence.Common.Abstractions;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Persistence.MySql.Infrastructure;
using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Testing;
using Compze.Testing.DbPool;
using Compze.Testing.DbPool.MicrosoftSql;
using Compze.Testing.DbPool.MySql;
using Compze.Testing.DbPool.PostgreSql;

namespace Compze.Tests.Integration.Internals.Testing.Persistence;

public abstract class DbPoolTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   internal static DbPool CreatePool() =>
      TestEnv.PersistenceLayer.Current switch
      {
         PersistenceLayer.MicrosoftSqlServer => new MsSqlDbPool(),
         PersistenceLayer.MySql => new MySqlDbPool(),
         PersistenceLayer.PostgreSql => new PgSqlDbPool(),
         PersistenceLayer.Memory => throw new ArgumentOutOfRangeException(),
         _ => throw new ArgumentOutOfRangeException()
      };

   internal static void UseConnection(string connectionString, DbPool pool, Action<ICompzeDbConnection> func)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.PostgreSql:
            UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.MySql:
            UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.Memory:
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