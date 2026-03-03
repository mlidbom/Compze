using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

class SqliteDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .CreatedBy(() => new SqliteDbPoolSqlLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

   readonly string _baseDirectory;
   // Without a unique name we can end up with another test re-creating a deleted database,
   // causing very odd behavior in tests that should just have exploded with the message that
   // their database is gone due to premature disposing of the pool. Yes, this really happened.
   readonly string _poolId = Guid.NewGuid().ToString();

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQLITE_DATABASE_POOL_BASE_DIRECTORY";

   SqliteDbPoolSqlLayer()
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
      using var connection = new SqliteConnection(ConnectionStringFor(db));
      SqliteConnection.ClearPool(connection);
      DeleteDbFile(db);
      using var dbCreatingConnection = new SqliteConnection(ConnectionStringFor(db));
      dbCreatingConnection.Open();
   }

   void DeleteDbFile(DbPoolDatabase db) => File.Delete(CreateDbPath(db)); //File.Delete does not throw on non-existent, files, so we can save one file system access by not checking for existence

   string CreateDbPath(DbPoolDatabase db) => Path.Combine(_baseDirectory, $"{db.Name}_{_poolId}.db");

   public void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
      // Sqlite sometimes takes a moment to release the files, so we retry in a loop
      const int maxCleanupAttempts = 1000;
      for(var attempt = 1; attempt <= maxCleanupAttempts; attempt++)
      {
         foreach(var db in reservedDatabases)
         {
            try
            {
               using var connection = new SqliteConnection(ConnectionStringFor(db));
               SqliteConnection.ClearPool(connection);
               DeleteDbFile(db);
               reservedDatabases = reservedDatabases.Where(it => it != db).ToList();
            }
            catch
            {
               if(attempt == maxCleanupAttempts)
               {
                  throw new Exception($"Failed to clean up database {CreateDbPath(db)}");
               }

               Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
         }

         if(reservedDatabases.Count == 0) break;
      }
   }
}
