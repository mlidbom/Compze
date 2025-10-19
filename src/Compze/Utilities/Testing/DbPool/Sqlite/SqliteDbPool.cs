using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      SqliteDbPool.RegisterWith(registrar);

   public static IComponentRegistrar DbPoolAndConnectionPoolForRandomConnectionString(this IComponentRegistrar registrar)
      => registrar.SqliteDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(Guid.NewGuid().ToString());

   public static IComponentRegistrar SqliteDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.SqliteDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

class SqliteDbPool : DbPoolBase
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
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

   public override void Dispose()
   {
      if(Disposed) return;

      // Sqlite sometimes takes a moment to release the files, so we retry in a loop
      const int maxCleanupAttempts = 1000;
      for(var attempt = 1; attempt <= maxCleanupAttempts; attempt++)
      {
         SqliteConnection.ClearAllPools();
         foreach(var db in _transientCache)
         {
            var dbPath = CreateDbPath(db);
            try
            {
               DeleteDb(dbPath);
               _transientCache = _transientCache.Where(it => it != db).ToList();
            }
            catch
            {
               if(attempt == maxCleanupAttempts)
               {
                  base.Dispose();
                  throw new Exception($"Failed to clean up database {dbPath}");
               }

               Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
         }
      }
      base.Dispose();
   }
}
