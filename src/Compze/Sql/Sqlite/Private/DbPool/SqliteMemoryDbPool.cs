using System.Collections.Generic;
using Compze.Sql.Common.DbPool;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Private.DbPool;

class SqliteMemoryDbPoolSqlLayer : IDbPoolSqlLayer
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .CreatedBy(() => new SqliteMemoryDbPoolSqlLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

   // Keep one connection open per database to prevent the in-memory database from disappearing when the last connection is closed
   readonly IThreadShared<IDictionary<string, SqliteConnection>> _keepInMemoryDatabaseAliveConnections = 
      IThreadShared.WithDefaultTimeout(new Dictionary<string, SqliteConnection>());

   public string ConnectionStringFor(DbPoolDatabase db)
   {
      return new SqliteConnectionStringBuilder
             {
                DataSource = db.Name,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
             }.ConnectionString;
   }

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) => ResetDatabase(db);
   public void ResetDatabase(DbPoolDatabase db) => _keepInMemoryDatabaseAliveConnections.Update(cons => cons[db.Name] = CreateOpenConnectionThusCreatingANewInMemoryDatabase(db));

   public void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
      _keepInMemoryDatabaseAliveConnections.Update(cons =>
      {
         cons.Values.ForEach(connection => connection.Dispose());
         cons.Clear();
      });
   }

   SqliteConnection CreateOpenConnectionThusCreatingANewInMemoryDatabase(DbPoolDatabase db)
   {
      var connectionToNewInMemoryDatabase = new SqliteConnection(ConnectionStringFor(db));
      connectionToNewInMemoryDatabase.Open();
      return connectionToNewInMemoryDatabase;
   }
}
