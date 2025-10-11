using Compze.Sql.Sqlite.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteDbPool : DbPool
{
   readonly string _baseDirectory;
   readonly HashSet<string> _createdDatabasePaths = [];

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQLITE_DATABASE_POOL_BASE_DIRECTORY";

   public SqliteDbPool()
   {
      _baseDirectory = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                    ?? Path.Combine(Path.GetTempPath(), "CompzeDbPool", "Sqlite");

      Directory.CreateDirectory(_baseDirectory);
   }

   protected override string ConnectionStringFor(Database db)
   {
      var dbPath = Path.Combine(_baseDirectory, $"{db.Name}.db");
      return new SqliteConnectionStringBuilder
      {
         DataSource = dbPath,
         Mode = SqliteOpenMode.ReadWriteCreate,
         Cache = SqliteCacheMode.Shared
      }.ConnectionString;
   }

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      var dbPath = Path.Combine(_baseDirectory, $"{db.Name}.db");
      _createdDatabasePaths.Add(dbPath);
      
      if(File.Exists(dbPath))
      {
         ResetDatabase(db);
      } else
      {
         // Create empty database
         using var connection = new SqliteConnection(ConnectionStringFor(db));
         connection.Open();
         connection.Close();
      }
   }

   protected override void ResetDatabase(Database db)
   {
      var dbPath = Path.Combine(_baseDirectory, $"{db.Name}.db");
      _createdDatabasePaths.Add(dbPath);
      
      // Close all connections
      ResetConnectionPool(db);
      
      // Delete and recreate the database file
      if(File.Exists(dbPath))
      {
         File.Delete(dbPath);
      }
      
      // Create fresh empty database
      using var connection = new SqliteConnection(ConnectionStringFor(db));
      connection.Open();
      connection.Close();
   }

   void ResetConnectionPool(Database db)
   {
      SqliteConnection.ClearAllPools();
   }

   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         // Clear all connection pools before deleting files
         SqliteConnection.ClearAllPools();
         
         // Delete all database files created by this pool
         foreach(var dbPath in _createdDatabasePaths.ToList())
         {
            try
            {
               if(File.Exists(dbPath))
               {
                  File.Delete(dbPath);
               }
               
               // Also delete the -wal and -shm files if they exist
               var walPath = $"{dbPath}-wal";
               if(File.Exists(walPath))
               {
                  File.Delete(walPath);
               }
               
               var shmPath = $"{dbPath}-shm";
               if(File.Exists(shmPath))
               {
                  File.Delete(shmPath);
               }
            }
            catch
            {
               // Ignore errors during cleanup - files might be locked or already deleted
            }
         }
         
         _createdDatabasePaths.Clear();
      }
      
      base.Dispose(disposing);
   }
}
