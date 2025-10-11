using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // For in-memory SQLite, we use shared cache with a unique database name.
   // The database is automatically created when the first connection opens,
   // and automatically destroyed when the last connection closes.
   // No explicit initialization, reset, or cleanup needed - everything is managed by connection lifecycle.
   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
      {
         DataSource = $"file:{db.Name}?mode=memory&cache=shared",
         Mode = SqliteOpenMode.ReadWriteCreate,
         Cache = SqliteCacheMode.Shared
      }.ConnectionString;
   }

   // In-memory database is created automatically on first connection
   protected override void EnsureDatabaseExistsAndIsEmpty(Database db) { }

   // In-memory database is destroyed automatically when all connections close
   protected override void ResetDatabase(Database db) { }
}
