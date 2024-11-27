using System;
using Composable.DependencyInjection;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.Persistence.DB2.Testing.Databases;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.Oracle.Testing.Databases;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using Composable.Testing;
using Composable.Testing.Databases;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests;

//[ConfigurationBasedDuplicateByDimensions]
public class DatabasePoolTest(string unknown) : DuplicateByPluggableComponentTest(unknown)
{
   internal static DatabasePool CreatePool() =>
      TestEnv.PersistenceLayer.Current switch
      {
         PersistenceLayer.MicrosoftSQLServer => new MsSqlDatabasePool(),
         PersistenceLayer.MySql => new MySqlDatabasePool(),
         PersistenceLayer.PostgreSql => new PgSqlDatabasePool(),
         PersistenceLayer.Oracle => new OracleDatabasePool(),
         PersistenceLayer.IBMDB2 => new DB2DatabasePool(),
         PersistenceLayer.Memory => throw new ArgumentOutOfRangeException(),
         _ => throw new ArgumentOutOfRangeException()
      };

   internal static void UseConnection(string connectionString, DatabasePool pool, Action<IComposableDbConnection> func)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSQLServer:
            UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.PostgreSql:
            UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.MySql:
            UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.Oracle:
            UseOracleConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.IBMDB2:
            UseComposableDB2Connection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.Memory:
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   static void UseMySqlConnection(string connectionStringFor, Action<IComposableDbConnection> func) =>
      IMySqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UsePgSqlConnection(string connectionStringFor, Action<IComposableDbConnection> func) =>
      IPgSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UseMsSqlConnection(string connectionStringFor, Action<IComposableDbConnection> func) =>
      IMsSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UseOracleConnection(string connectionStringFor, Action<IComposableDbConnection> func) =>
      IOracleConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UseComposableDB2Connection(string connectionStringFor, Action<IComposableDbConnection> func) =>
      IDB2ConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);
}