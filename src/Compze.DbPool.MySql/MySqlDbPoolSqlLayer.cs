using Compze.Underscore;
using Compze.Internals.Sql.MySql;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
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

   ///<summary>How long a database reset's <c>DROP DATABASE</c> waits for the schema metadata lock before failing, rather than<br/>
   /// blocking until a connection timeout when a leaked transaction still holds it. Short: a reset of a genuinely free database<br/>
   /// waits for nothing, so this only bounds the pathological case.</summary>
   const int ResetLockWaitTimeoutSeconds = 3;

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

      //A short lock_wait_timeout so a DROP blocked on a metadata lock held by a leaked transaction fails fast (seconds) rather
      //than hanging until a connection timeout. The pool then abandons this database and reserves another, so one stray
      //transaction can never gridlock the pool (see Compze.DbPool.DbPool.ConnectionStringFor). Session-scoped: it rides the same
      //connection as the DROP/CREATE below and never leaks to any other use of this pooled connection.
      _masterConnectionPool.ExecuteNonQuery($"""
                                             SET SESSION lock_wait_timeout = {ResetLockWaitTimeoutSeconds};
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
