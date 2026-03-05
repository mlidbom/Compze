using Compze.Core.Wiring.Testing.Internal;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.Sqlite;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DbPool.Tests;

public abstract class DbPoolTestBase : UniversalTestBase
{
   protected DbPool Pool  { get; }
   protected override void DisposeInternal() => Pool.Dispose();
   readonly IServiceLocator _serviceLocator;

   protected DbPoolTestBase()
   {
      _serviceLocator = CreateServiceLocator();
      Pool = ResolvePool();
   }

#pragma warning disable CA2000// We are passing this disposable into a constructor of an object we don't own
   protected static IServiceLocator CreateServiceLocator() => TestEnv.DIContainer.CreateEmpty()
                                                                     ._mutate(it => it.Register()
                                                                                     .CurrentTestsDbPoolIfNotCloneContainer())
                                                                     .ServiceLocator;
#pragma warning restore CA2000// We are passing this disposable into a constructor of an object we don't own

   protected override async Task DisposeAsyncInternal() => await _serviceLocator.DisposeAsync();

   protected DbPool ResolvePool() =>
      _serviceLocator.Resolve<DbPool>();

   protected static void UseConnection(string connectionString, DbPool pool, Action<ICompzeDbConnection> func)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MsSql:
            UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case SqlLayer.PgSql:
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
