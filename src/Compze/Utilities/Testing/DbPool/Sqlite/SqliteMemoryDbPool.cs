using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.ResourceAccess;
using Microsoft.Data.Sqlite;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      SqliteMemoryDbPool.RegisterWith(registrar);

   public static IComponentRegistrar SqliteMemoryDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.SqliteMemoryDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteMemoryDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

class SqliteMemoryDbPool : DbPoolBase
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<SqliteMemoryDbPool>())
         return registrar;

      return registrar.Register(Singleton.For<SqliteMemoryDbPool>()
                                         .CreatedBy(() => new SqliteMemoryDbPool())
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

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

   public override void Dispose()
   {
      if(Disposed) return;
      base.Dispose();
      _keepInMemoryDatabaseAliveConnections.Update(cons =>
      {
         cons.Values.ForEach(connection => connection.Dispose());
         cons.Clear();
      });
   }

   SqliteConnection CreateOpenConnectionThusCreatingANewInMemoryDatabase(Database db)
   {
      var connectionToNewInMemoryDatabase = new SqliteConnection(ConnectionStringFor(db));
      connectionToNewInMemoryDatabase.Open();
      return connectionToNewInMemoryDatabase;
   }
}
