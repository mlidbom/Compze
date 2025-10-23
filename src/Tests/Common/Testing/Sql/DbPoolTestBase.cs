using System;
using System.Threading.Tasks;
using Compze.Sql.Common.Abstractions;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.PostgreSql;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tests.Common.Testing.Sql;

public abstract class DbPoolTestBase : UniversalTestBase
{
   protected readonly DbPoolBase Pool;
   protected override void DisposeInternal() => Pool.Dispose();
   readonly IServiceLocator _serviceLocator;

   protected DbPoolTestBase()
   {
      _serviceLocator = CreateServiceLocator();
      Pool = ResolvePool();
   }

   protected IServiceLocator CreateServiceLocator() => TestEnv.DIContainer.CreateServiceLocatorForTesting(_ => {});

   protected override async Task DisposeAsyncInternal() => await _serviceLocator.DisposeAsync();

   protected DbPoolBase ResolvePool() =>
      _serviceLocator.Resolve<DbPoolBase>();

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
