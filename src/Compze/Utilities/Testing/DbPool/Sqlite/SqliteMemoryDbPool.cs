using Compze.Sql.Sqlite.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // For in-memory SQLite, we use shared cache with a unique database name
   // This allows multiple connections to access the same in-memory database
   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
      {
         DataSource = $"file:{db.Name}?mode=memory&cache=shared",
         Mode = SqliteOpenMode.ReadWriteCreate,
         Cache = SqliteCacheMode.Shared
      }.ConnectionString;
   }

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      // In-memory databases are created automatically when opened
      // Just ensure we have a connection to create the database
      using var connection = new SqliteConnection(ConnectionStringFor(db));
      connection.Open();
      // Database is created, close the connection
      connection.Close();
   }

   protected override void ResetDatabase(Database db)
   {
      // For in-memory databases, we need to close all connections and clear pools
      // This effectively destroys the in-memory database
      SqliteConnection.ClearAllPools();
      
      // Recreate by opening a new connection
      using var connection = new SqliteConnection(ConnectionStringFor(db));
      connection.Open();
      connection.Close();
   }

   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         // Clear all connection pools - this will destroy all in-memory databases
         SqliteConnection.ClearAllPools();
      }
      
      base.Dispose(disposing);
   }
}
