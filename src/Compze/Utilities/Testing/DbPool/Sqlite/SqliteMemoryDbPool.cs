using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // Keep one connection open per database to prevent the in-memory database from disappearing when the last connection is closed
   readonly ConcurrentDictionary<string, SqliteConnection> _keepInMemoryDatabaseAliveConnections = new();

   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
             {
                DataSource = $"file:{db.Name}?mode=memory&cache=shared",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db) => _keepInMemoryDatabaseAliveConnections[db.Name] = CreateOpenConnectionThusCreatingANewInMemoryDatabase(db);
   protected override void ResetDatabase(Database db) => _keepInMemoryDatabaseAliveConnections[db.Name] = CreateOpenConnectionThusCreatingANewInMemoryDatabase(db);

   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         _keepInMemoryDatabaseAliveConnections.Values.ForEach(connection => connection.Dispose());
         _keepInMemoryDatabaseAliveConnections.Clear();
      }

      base.Dispose(disposing);
   }

   SqliteConnection CreateOpenConnectionThusCreatingANewInMemoryDatabase(Database db)
   {
      var keeperConnection = new SqliteConnection(ConnectionStringFor(db));
      keeperConnection.Open();
      return keeperConnection;
   }
}
