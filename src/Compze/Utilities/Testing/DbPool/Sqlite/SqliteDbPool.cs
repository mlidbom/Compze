using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

static class SqliteDbPoolRegistrar
{
   public static IDependencyRegistrar SqliteDbPoolIfNotAlreadyRegistered(this IDependencyRegistrar registrar) =>
      SqliteDbPool.RegisterWith(registrar);
}

class SqliteDbPool : DbPoolBase
{
   internal static IDependencyRegistrar RegisterWith(IDependencyRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<SqliteDbPool>())
         return registrar;

      return registrar.Register(Singleton.For<SqliteDbPool>()
                                         .CreatedBy(() => new SqliteDbPool())
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly string _baseDirectory;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQLITE_DATABASE_POOL_BASE_DIRECTORY";

   internal SqliteDbPool()
   {
      _baseDirectory = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                    ?? Path.Combine(Path.GetTempPath(), "CompzeDbPool", "Sqlite");

      Directory.CreateDirectory(_baseDirectory);
   }

   protected override string ConnectionStringFor(Database db)
   {
      var dbPath = CreateDbPath(db);
      return new SqliteConnectionStringBuilder
             {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db) => ResetDatabase(db);

   protected override void ResetDatabase(Database db)
   {
      using var dbCreatingConnection = new SqliteConnection(ConnectionStringFor(db));
      dbCreatingConnection.Open();
   }

   static void DeleteDb(string dbPath)
   {
      if(File.Exists(dbPath))
      {
         File.Delete(dbPath);
      }
   }

   string CreateDbPath(Database db) => Path.Combine(_baseDirectory, $"{db.Name}.db");

   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         // Sqlite sometimes takes a moment to release the files, so we retry a few times
         const int MaxCleanupAttempts = 10;
         for(int attempt = 1; attempt <= MaxCleanupAttempts; attempt++)
         {
            SqliteConnection.ClearAllPools();
            foreach(var db in _transientCache)
            {
               var dbPath = CreateDbPath(db);
               try
               {
                  DeleteDb(dbPath);
               }
               catch
               {
                  if(attempt == MaxCleanupAttempts)
                  {
                     throw new Exception($"Failed to clean up database {dbPath}");
                  }

                  Thread.Sleep(10);
               }
            }
         }

         base.Dispose(disposing);
      }
   }
}
