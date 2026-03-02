using System;
using Compze.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.SqlClient;

namespace Compze.Utilities.Testing.DbPool.MicrosoftSql;

class MsSqlDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .CreatedBy(() => new MsSqlDbPoolSqlLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

   readonly string _masterConnectionString;
   readonly IMsSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   MsSqlDbPoolSqlLayer()
   {
      _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                             ?? "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";

      _masterConnectionPool = IMsSqlConnectionPool.CreateInstance(_masterConnectionString);
   }

   public string ConnectionStringFor(DbPoolDatabase db)
      => new SqlConnectionStringBuilder(_masterConnectionString) { InitialCatalog = db.Name }.ConnectionString;

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

   void ResetConnectionPool(DbPoolDatabase db)
   {
      using var connection = new SqlConnection(ConnectionStringFor(db));
      SqlConnection.ClearPool(connection);
   }
}
