using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Data.Sqlite;

namespace Compze.DbPool.Sqlite;

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
      Delete(db);
      using var dbCreatingConnection = new SqliteConnection(ConnectionStringFor(db));
      dbCreatingConnection.Open();
   }

   string CreateDbPath(DbPoolDatabase db) => Path.Combine(_baseDirectory, $"{db.Name}_{_poolId}.db");

   public void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases) => reservedDatabases.ForEach(Delete);

   void Delete(DbPoolDatabase db)
   {
      // Sqlite sometimes takes a moment to release the files, so we retry in a loop for a maximum of 10 seconds
      var startTime = DateTime.UtcNow;
      while(true)
      {
         try
         {
            using var connection = new SqliteConnection(ConnectionStringFor(db));
            SqliteConnection.ClearPool(connection);
            File.Delete(CreateDbPath(db));
            return;
         }
         catch(Exception ex)
         {
            if(DateTime.UtcNow - startTime > 10.Seconds())
            {
               throw new Exception($"Failed to clean up database {CreateDbPath(db)}", ex);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(10));
         }
      }
   }

}
