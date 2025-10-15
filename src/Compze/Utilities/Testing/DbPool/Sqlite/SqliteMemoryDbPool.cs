using Microsoft.Data.Sqlite;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

internal class SqliteMemoryDbPool : DbPool
{
   // Keep one connection open per database to prevent the in-memory database from disappearing when the last connection is closed
   readonly IThreadShared<IDictionary<string, SqliteConnection>> _keepInMemoryDatabaseAliveConnections = ThreadShared.WithDefaultTimeout(new Dictionary<string, SqliteConnection>());

   protected override string ConnectionStringFor(Database db)
   {
      return new SqliteConnectionStringBuilder
             {
                DataSource = $"file:{db.Name}?mode=memory&cache=shared",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db) => ResetDatabase(db);
   protected override void ResetDatabase(Database db) => _keepInMemoryDatabaseAliveConnections.Update(cons => cons[db.Name] = CreateOpenConnectionThusCreatingANewInMemoryDatabase(db));

   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         _keepInMemoryDatabaseAliveConnections.Update(cons =>
         {
            cons.Values.ForEach(connection => connection.Dispose());
            cons.Clear();
         });
      }

      base.Dispose(disposing);
   }

   SqliteConnection CreateOpenConnectionThusCreatingANewInMemoryDatabase(Database db)
   {
      var connectionToNewInMemoryDatabase = new SqliteConnection(ConnectionStringFor(db));
      connectionToNewInMemoryDatabase.Open();
      return connectionToNewInMemoryDatabase;
   }
}
