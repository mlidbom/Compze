﻿using System;
using Compze.Persistence.MsSql.SystemExtensions;
using Compze.Persistence.MsSql.Testing.Databases;
using Microsoft.Data.SqlClient;

namespace Compze.Testing.Persistence.MsSql;

class MsSqlDbPool : DbPool
{
   readonly string _masterConnectionString;
   readonly IMsSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   public MsSqlDbPool()
   {
      _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                             ?? "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";

      _masterConnectionPool = IMsSqlConnectionPool.CreateInstance(_masterConnectionString);
   }

   protected override string ConnectionStringFor(Database db)
      => new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = db.Name}.ConnectionString;

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
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

   protected override void ResetDatabase(Database db) =>
      IMsSqlConnectionPool.CreateInstance(ConnectionStringFor(db))
                          .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel());

   protected void ResetConnectionPool(Database db)
   {
      using var connection = new SqlConnection(ConnectionStringFor(db));
      SqlConnection.ClearPool(connection);
   }
}