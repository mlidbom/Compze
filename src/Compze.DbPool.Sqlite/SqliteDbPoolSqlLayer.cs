using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Data.Sqlite;

namespace Compze.DbPool.Sqlite;

class SqliteDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy(() => new SqliteDbPoolSqlLayer()));

   readonly DirectoryInfo _baseDirectory;

   // Without a unique name we can end up with another test re-creating a deleted database,
   // causing very odd behavior in tests that should just have exploded with the message that
   // their database is gone due to premature disposing of the pool. Yes, this really happened.
   readonly string _poolId = Guid.NewGuid().ToString();

   const string DbDirectoryEnvironmentVariableName = "COMPOSABLE_SQLITE_DATABASE_POOL_BASE_DIRECTORY";

   SqliteDbPoolSqlLayer()
   {
      _baseDirectory = new DirectoryInfo(
         Environment.GetEnvironmentVariable(DbDirectoryEnvironmentVariableName)
      ?? Path.Combine(Path.GetTempPath(), "CompzeDbPool", "Sqlite"));

      _baseDirectory.Create();
   }

   public string ConnectionStringFor(DbPoolDatabase db)
   {
      var dbFile = FileInfoFor(db);
      return new SqliteConnectionStringBuilder
             {
                DataSource = dbFile.FullName,
                Mode = SqliteOpenMode.ReadWriteCreate
             }.ConnectionString;
   }

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) => ResetDatabase(db);

   public void ResetDatabase(DbPoolDatabase db)
   {
      Delete(db);
      using var dbCreatingConnection = new SqliteConnection(ConnectionStringFor(db));
      dbCreatingConnection.Open();
   }

   FileInfo FileInfoFor(DbPoolDatabase db) => _baseDirectory.File($"{db.Name}_{_poolId}.db");

   // Deletes the database file together with the WAL sidecar files. A leftover -wal/-shm from a deleted database
   // would otherwise be recovered into the next database created with the same name, resurrecting stale data.
   // FileInfo.Delete() is a no-op when the file is absent, so deleting sidecars that were never created is safe.
   void DeleteDatabaseFiles(DbPoolDatabase db)
   {
      var databaseFile = FileInfoFor(db);
      databaseFile.Delete();
      _baseDirectory.File($"{databaseFile.Name}-wal").Delete();
      _baseDirectory.File($"{databaseFile.Name}-shm").Delete();
   }

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
            DeleteDatabaseFiles(db);
            return;
         }
         catch(Exception ex)
         {
            if(DateTime.UtcNow - startTime > 10.Seconds())
            {
               throw new Exception($"Failed to clean up database {FileInfoFor(db).FullName}", ex);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(10));
         }
      }
   }

}
