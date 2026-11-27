using Compze.Sql.Common._internal;
using Compze.Sql.MicrosoftSql._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Microsoft.Data.SqlClient;

namespace Compze.DbPool.MicrosoftSql._private;

class MsSqlDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy(() => new MsSqlDbPoolSqlLayer()));

   readonly string _masterConnectionString;
   readonly IMsSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   MsSqlDbPoolSqlLayer()
   {
      _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                             ?? "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";

      _masterConnectionPool = IMsSqlConnectionPool.CreateInstance(_masterConnectionString);
   }

   ///<summary>The connection string of <paramref name="db"/>, with a pool of ten connections, as the test databases of PostgreSQL and<br/>
   /// MySQL have: a test that exhausts the pool exhausts it on all three.</summary>
   public string ConnectionStringFor(DbPoolDatabase db)
      => new SqlConnectionStringBuilder(_masterConnectionString) { InitialCatalog = db.Name, MaxPoolSize = 10 }.ConnectionString;

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db)
   {
      var databaseName = db.Name;
      var exists = (string?)_masterConnectionPool.ExecuteScalar($"select name from sysdatabases where name = '{databaseName}'") == databaseName;
      if(exists)
      {
         ResetDatabase(db);
      } else
      {
         _masterConnectionPool.ExecuteNonQuery($"""
                                                CREATE DATABASE [{databaseName}]
                                                ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
                                                ALTER DATABASE[{databaseName}] SET READ_COMMITTED_SNAPSHOT ON
                                                """);
      }
   }

   public void ResetDatabase(DbPoolDatabase db)
   {
      ResetConnectionPool(db); // Clear stale connections before DDL
      try
      {
         IMsSqlConnectionPool.CreateInstance(ConnectionStringFor(db))
                             .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel());
      }
      catch
      {
         ResetConnectionPool(db); //The connection pool has cached the belief that this database does not exist. We must clear that.
         throw;
      }
   }

   ///<summary>Clears every pool Compze keeps in this process on <paramref name="db"/>: the database's own, and the pools of their own on it.</summary>
   void ResetConnectionPool(DbPoolDatabase db)
   {
      foreach(var connectionString in PoolsOfTheirOwn.ConnectionStringsOfEveryPoolOn(ConnectionStringFor(db)))
      {
         using var connection = new SqlConnection(connectionString);
         SqlConnection.ClearPool(connection);
      }
   }
}
