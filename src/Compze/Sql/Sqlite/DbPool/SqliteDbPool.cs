using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

class SqliteDbPoolSqlLayer : IDbPoolSqlLayer
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<IDbPoolSqlLayer>())
         return registrar;

      return registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                         .CreatedBy(() => new SqliteDbPoolSqlLayer())
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly string _baseDirectory;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQLITE_DATABASE_POOL_BASE_DIRECTORY";

   internal SqliteDbPoolSqlLayer()
   {
      _baseDirectory = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                    ?? Path.Combine(Path.GetTempPath(), "CompzeDbPool", "Sqlite");

      Directory.CreateDirectory(_baseDirectory);
   }

   public string ConnectionStringFor(DbPoolDatabase db)
   {
      var dbPath = CreateDbPath(db);
      return new SqliteConnectionStringBuilder
             {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) => ResetDatabase(db);

   public void ResetDatabase(DbPoolDatabase db)
   {
      using var dbCreatingConnection = new SqliteConnection(ConnectionStringFor(db));
      dbCreatingConnection.Open();
   }

   static void DeleteDb(string dbPath)
   {
      if(File.Exists(dbPath))
      {
         File.Delete(dbPath);
      }
   }

   string CreateDbPath(DbPoolDatabase db) => Path.Combine(_baseDirectory, $"{db.Name}.db");

   public void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
      // Sqlite sometimes takes a moment to release the files, so we retry in a loop
      const int maxCleanupAttempts = 1000;
      for(var attempt = 1; attempt <= maxCleanupAttempts; attempt++)
      {
         SqliteConnection.ClearAllPools();
         foreach(var db in reservedDatabases)
         {
            var dbPath = CreateDbPath(db);
            try
            {
               DeleteDb(dbPath);
               reservedDatabases = reservedDatabases.Where(it => it != db).ToList();
            }
            catch
            {
               if(attempt == maxCleanupAttempts)
               {
                  throw new Exception($"Failed to clean up database {dbPath}");
               }

               Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
         }
      }
   }
}
