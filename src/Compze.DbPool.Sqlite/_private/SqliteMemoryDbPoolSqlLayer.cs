using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading.ResourceAccess;
using Microsoft.Data.Sqlite;

namespace Compze.DbPool.Sqlite._private;

class SqliteMemoryDbPoolSqlLayer : IDbPoolSqlLayer
{
   readonly string _poolId = Guid.NewGuid().ToString();
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy(() => new SqliteMemoryDbPoolSqlLayer()));

   // Keep one connection open per database to prevent the in-memory database from disappearing when the last connection is closed
   readonly IThreadShared<IDictionary<string, SqliteConnection>> _keepInMemoryDatabaseAliveConnections =
      IThreadShared.New(new Dictionary<string, SqliteConnection>());

   ///<summary>Many more than a server's 100: an in-memory database costs memory only while a pool holds it, and is private to that pool, so<br/>
   /// nothing but the most databases a test suite holds at once calls for a limit. Compze's suite held 152 at once on a machine with 32 logical<br/>
   /// processors in November 2026, and 100 made its tests time out.</summary>
   public int MaximumNumberOfDatabases => 1000;

   public string ConnectionStringFor(DbPoolDatabase db)
   {
      return new SqliteConnectionStringBuilder
             {
                //In case the cleanup in dispose is not successful in getting rid of the database (say for instance some code somewhere is keeping a connection open, thus keeping the database alive across pool instances)
                //,we want a unique connection string so that we never get an existing in memory database for a new pool, breaking test isolation
                DataSource = $"{db.Name}_pool_id_{_poolId}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) => ResetDatabase(db);
   public void ResetDatabase(DbPoolDatabase db) => _keepInMemoryDatabaseAliveConnections.Locked(cons =>
   {
      if(cons.TryGetValue(db.Name, out var oldConnection))
      {
         SqliteConnection.ClearPool(oldConnection);
         oldConnection.Dispose();
      }
      cons[db.Name] = CreateOpenConnectionThusCreatingANewInMemoryDatabase(db);
   });

   public void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
      _keepInMemoryDatabaseAliveConnections.Locked(keepAliveConnections =>
      {
         keepAliveConnections.Values.ForEach(connection =>
         {
            SqliteConnection.ClearPool(connection);
            connection.Dispose();
         });
         keepAliveConnections.Clear();
      });
   }

   SqliteConnection CreateOpenConnectionThusCreatingANewInMemoryDatabase(DbPoolDatabase db)
   {
      var connectionToNewInMemoryDatabase = new SqliteConnection(ConnectionStringFor(db));
      connectionToNewInMemoryDatabase.Open();
      return connectionToNewInMemoryDatabase;
   }
}
