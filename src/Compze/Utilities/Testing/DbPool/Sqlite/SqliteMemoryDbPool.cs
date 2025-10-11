using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // Keep one connection open per database to prevent the in-memory database from disappearing
   // Key: database name, Value: keeper connection
   readonly ConcurrentDictionary<string, SqliteConnection> _keeperConnections = new();

   // For in-memory SQLite testing with shared cache, the database only exists while
   // at least one connection to it is open. We must keep a "keeper" connection open
   // for the lifetime of each database to prevent it from disappearing between uses.
   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
      {
         DataSource = $"file:{db.Name}?mode=memory&cache=shared",
         Mode = SqliteOpenMode.Memory,
         Cache = SqliteCacheMode.Shared
      }.ConnectionString;
   }

   // Open and keep a connection to ensure the database stays alive
   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      var connectionString = ConnectionStringFor(db);
      var keeperConnection = new SqliteConnection(connectionString);
      keeperConnection.Open();
      
      // Store the keeper connection - this keeps the in-memory database alive
      _keeperConnections[db.Name] = keeperConnection;
   }

   // To reset an in-memory database, we must close the keeper connection (destroying the DB)
   // and then open a new keeper connection (creating a fresh empty DB)
   protected override void ResetDatabase(Database db)
   {
      // Close the keeper connection, which destroys the in-memory database
      if (_keeperConnections.TryRemove(db.Name, out var oldConnection))
      {
         oldConnection.Dispose();
      }
      
      // Clear the connection pool for this specific database
      // This ensures no pooled connections remain that reference the old database
      SqliteConnection.ClearAllPools();
      
      // Open a new keeper connection, creating a fresh empty in-memory database
      var connectionString = ConnectionStringFor(db);
      var newConnection = new SqliteConnection(connectionString);
      newConnection.Open();
      _keeperConnections[db.Name] = newConnection;
   }

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         // Close all keeper connections
         foreach (var connection in _keeperConnections.Values)
         {
            connection.Dispose();
         }
         _keeperConnections.Clear();
      }
      
      base.Dispose(disposing);
   }
}
