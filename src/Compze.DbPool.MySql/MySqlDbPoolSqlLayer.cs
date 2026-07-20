using Compze.Underscore;
using Compze.Internals.Sql.MySql;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Threading.ResourceAccess;
using MySqlConnector;

namespace Compze.DbPool.MySql;

sealed class MySqlDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .DelegateToParentServiceLocatorWhenCloning()
                                  .CreatedBy(() => new MySqlDbPoolSqlLayer()));

   readonly IMySqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   readonly IThreadShared<MySqlConnectionStringBuilder> _connectionStringBuilder;

   MySqlDbPoolSqlLayer()
   {
      var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                ?? "Server=localhost;Database=mysql;Uid=root;Pwd=Development!1;";

      _masterConnectionPool = IMySqlConnectionPool.CreateInstance(masterConnectionString);
      _connectionStringBuilder = IThreadShared.New(new MySqlConnectionStringBuilder(masterConnectionString));
   }

   public string ConnectionStringFor(DbPoolDatabase db)
      => _connectionStringBuilder.Locked(it => it._mutate(me =>
      {
         me.Database = db.Name;
         me.MinimumPoolSize = 1;
         me.MaximumPoolSize = 10;
         me.ConnectionLifeTime = 10;
      }).ConnectionString);

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db) => ResetDatabase(db);

   public void ResetDatabase(DbPoolDatabase db)
   {
      ResetConnectionPool(db);
      //I experimented with dropping objects like for the other databases, but it was not faster than just dropping and recreating the database.
      _masterConnectionPool.ExecuteNonQuery($"""
                                             DROP DATABASE IF EXISTS {db.Name};
                                             CREATE DATABASE {db.Name};
                                             """);
   }

   void ResetConnectionPool(DbPoolDatabase db)
   {
      using var connection = new MySqlConnection(ConnectionStringFor(db));
      MySqlConnection.ClearPool(connection);
   }
}
