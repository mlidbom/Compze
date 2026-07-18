using Compze.Underscore;
using Compze.Internals.Sql.MySql;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Threading.ResourceAccess;
using MySql.Data.MySqlClient;

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

      //rationale: TEMPORARY diagnostics for the CI MySql DbPool-reset hang (branch re-merge-tessaging-and-typermedia). A
      //DROP/CREATE DATABASE that blocks for ~60s and then dies with SocketException(110) points at a lock wait, not the server
      //being slow. This watchdog captures - from a SEPARATE connection while the reset is still blocked - who holds the
      //server's locks, so we can name the blocking session. Remove once root-caused.
      using var resetFinished = new CancellationTokenSource();
      var diagnostics = CaptureServerLockStateIfResetBlocks(db, resetFinished.Token);
      try
      {
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
      finally
      {
         resetFinished.Cancel();
         diagnostics.Wait(TimeSpan.FromSeconds(15));
      }
   }

   void ResetConnectionPool(DbPoolDatabase db)
   {
      using var connection = new MySqlConnection(ConnectionStringFor(db));
      MySqlConnection.ClearPool(connection);
   }

   //rationale: TEMPORARY diagnostics - see ResetDatabase. Remove once the reset hang is root-caused.
   Task CaptureServerLockStateIfResetBlocks(DbPoolDatabase db, CancellationToken resetFinished) =>
      Task.Run(() =>
      {
         if(resetFinished.WaitHandle.WaitOne(TimeSpan.FromSeconds(5))) return; //Reset completed promptly - nothing to diagnose.

         try
         {
            var (processList, metadataLocks) = _masterConnectionPool.UseConnection(connection =>
            {
               connection.ExecuteNonQuery("SET SESSION group_concat_max_len = 1000000;");
               return (processList: connection.ExecuteScalar(ProcessListDiagnosticSql) as string,
                       metadataLocks: connection.ExecuteScalar(MetadataLockDiagnosticSql) as string);
            });

            this.Log().Warning($"""
                                DbPool reset of database '{db.Name}' has been blocked for over 5 seconds. Server-side state captured from a separate connection:
                                === information_schema.PROCESSLIST (id | user | host | db | command | time | state | info) ===
                                {processList ?? "(none)"}
                                === performance_schema.metadata_locks (object_type | schema | object | lock_type | lock_status | owner_processlist_id) ===
                                {metadataLocks ?? "(none)"}
                                """);
         }
#pragma warning disable CA1031 //Diagnostics-only: a capture failure (e.g. the server itself being unreachable) is itself the informative outcome and must not mask the original hang.
         catch(Exception captureException)
         {
#pragma warning restore CA1031
            this.Log().Warning(captureException, $"DbPool reset of database '{db.Name}' is blocked, AND the diagnostic capture from a separate connection also failed. That is consistent with the MySql server being unreachable rather than lock-blocked.");
         }
      }, CancellationToken.None); //The token is consumed inside the body as the reset-finished signal, not as Task.Run's own start-cancellation.

   const string ProcessListDiagnosticSql =
      """
      SELECT GROUP_CONCAT(CONCAT_WS(' | ', ID, USER, HOST, COALESCE(DB,''), COMMAND, TIME, COALESCE(STATE,''), LEFT(COALESCE(INFO,''), 200)) ORDER BY TIME DESC SEPARATOR '\n')
      FROM information_schema.PROCESSLIST;
      """;

   const string MetadataLockDiagnosticSql =
      """
      SELECT GROUP_CONCAT(CONCAT_WS(' | ', ml.OBJECT_TYPE, COALESCE(ml.OBJECT_SCHEMA,''), COALESCE(ml.OBJECT_NAME,''), ml.LOCK_TYPE, ml.LOCK_STATUS, COALESCE(t.PROCESSLIST_ID,'?')) SEPARATOR '\n')
      FROM performance_schema.metadata_locks ml
      LEFT JOIN performance_schema.threads t ON t.THREAD_ID = ml.OWNER_THREAD_ID;
      """;
}
