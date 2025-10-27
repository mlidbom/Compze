using System;
using Compze.Sql.Common.DbPool;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql.Private.DbPool;

class MsSqlDbPoolSqlLayer : IDbPoolSqlLayer
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .CreatedBy(() => new MsSqlDbPoolSqlLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

   readonly string _masterConnectionString;
   readonly IMsSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   public MsSqlDbPoolSqlLayer()
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
         ResetConnectionPool(db);
         var createDatabaseCommand = $"""
                                      CREATE DATABASE [{databaseName}]
                                      ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
                                      ALTER DATABASE[{databaseName}] SET READ_COMMITTED_SNAPSHOT ON
                                      """;

         _masterConnectionPool.ExecuteNonQuery(createDatabaseCommand);
      }
   }

   public void ResetDatabase(DbPoolDatabase db) =>
      IMsSqlConnectionPool.CreateInstance(ConnectionStringFor(db))
                          .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel());

   protected void ResetConnectionPool(DbPoolDatabase db)
   {
      using var connection = new SqlConnection(ConnectionStringFor(db));
      SqlConnection.ClearPool(connection);
   }
}
