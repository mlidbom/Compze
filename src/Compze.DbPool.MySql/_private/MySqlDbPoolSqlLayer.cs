using Compze.Sql.Common._internal;
using Compze.Underscore;
using Compze.Sql.MySql._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Threading.ResourceAccess;
using MySqlConnector;

namespace Compze.DbPool.MySql._private;

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
      //performance: in a full suite run, this waits seconds for the server, which the suite saturates with DDL: with
      //   InnoDB's defaults, a pool database's use, a test's CREATE TABLE statements and this drop, is about 440 syncs
      //   to disk. Dropping the tables instead, as the other layers drop their objects, is no faster; emptying them and
      //   keeping them, with DELETE, gave 23 times the database uses a second, and isn't decided. Both measured in
      //   dev_docs/why-resets-take-seconds-on-mysql.md.
      _masterConnectionPool.ExecuteNonQuery($"""
                                             DROP DATABASE IF EXISTS {db.Name};
                                             CREATE DATABASE {db.Name};
                                             """);
   }

   ///<summary>Clears every pool Compze keeps in this process on <paramref name="db"/>: the database's own, and the pools of their own on it.</summary>
   void ResetConnectionPool(DbPoolDatabase db)
   {
      foreach(var connectionString in PoolsOfTheirOwn.ConnectionStringsOfEveryPoolOn(CompzeMySqlConnectionString.From(ConnectionStringFor(db))))
      {
         using var connection = new MySqlConnection(connectionString);
         MySqlConnection.ClearPool(connection);
      }
   }
}
