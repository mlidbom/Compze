using Compze.Sql.Sqlite.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteDbPool : DbPool
{
   readonly string _baseDirectory;

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
}
