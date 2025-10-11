using Compze.Sql.Sqlite.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // For in-memory SQLite, we use shared cache with a unique database name.
   // This allows multiple connections to access the same in-memory database.
   // The database is automatically created when the first connection opens,
   // and automatically destroyed when the last connection closes.
   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
      {
         DataSource = $"file:{db.Name}?mode=memory&cache=shared",
         Mode = SqliteOpenMode.ReadWriteCreate,
         Cache = SqliteCacheMode.Shared
      }.ConnectionString;
   }

   // In-memory databases are created automatically when the first connection opens.
   // No explicit initialization needed.
   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      // Nothing to do - in-memory DB is created automatically
   }

   // In-memory databases with shared cache are automatically destroyed when all connections close.
   // Clearing the connection pool ensures all connections are closed.
   protected override void ResetDatabase(Database db)
   {
      SqliteConnection.ClearAllPools();
   }

   // Cleanup: Clear all connection pools to destroy all in-memory databases
   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         SqliteConnection.ClearAllPools();
      }
      
      base.Dispose(disposing);
   }
}
