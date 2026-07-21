using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading.ResourceAccess;
using Microsoft.Data.Sqlite;

namespace Compze.DbPool.Sqlite.Private;

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
