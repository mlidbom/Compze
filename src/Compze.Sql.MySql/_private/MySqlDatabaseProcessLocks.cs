using System.Data.Common;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.MySql._internal;
using MySqlConnector;
using Holders = Compze.Sql.Common._internal.ProcessLocks.DatabaseProcessLockHolderSchemaStrings;

namespace Compze.Sql.MySql._private;

///<summary>The process locks of a MySQL database: each a named lock (<c>GET_LOCK</c>), owned by the session that takes it. MySQL's lock names are<br/>
/// server-wide and at most 64 characters long, so each is hashed from the database's name and the lock's.</summary>
sealed class MySqlDatabaseProcessLocks : DatabaseServerProcessLocks
{
   internal static readonly string SchemaCreationSql =
      $"""

        CREATE TABLE IF NOT EXISTS {Holders.TableName}
        (
            {Holders.LockName} VARCHAR({Holders.LockNameLength}) NOT NULL,
            {Holders.Holder}   VARCHAR({Holders.HolderLength})   NOT NULL,

            PRIMARY KEY ( {Holders.LockName} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

       """;

   readonly IMySqlConnectionPool _connectionPool;
   readonly MySqlSqlLayerSchemaManager _schemaManager;

   internal MySqlDatabaseProcessLocks(IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

   protected override DbConnection NewSessionOfItsOwn() =>
      new MySqlConnection(new MySqlConnectionStringBuilder(_connectionPool.ConnectionString) { Pooling = false, AutoEnlist = false }.ConnectionString);

   protected override void EnsureTheHolderTableExists() => _schemaManager.EnsureSchemaInitialized();

   ///<remarks>GET_LOCK answers 1 when it took the lock and 0 when another session holds it: NULL, for an error, fails the take.</remarks>
   protected override void SetUpTheTry(DbCommand command, string lockName)
   {
      command.CommandText = "SELECT GET_LOCK(@LockName, 0)";
      AddParameter(command, "LockName", NameOf(command, lockName));
   }

   protected override void SetUpTheRelease(DbCommand command, string lockName)
   {
      command.CommandText = "SELECT RELEASE_LOCK(@LockName)";
      AddParameter(command, "LockName", NameOf(command, lockName));
   }

   protected override string RecordTheHolderSql =>
      $"""
       INSERT INTO {Holders.TableName} ({Holders.LockName}, {Holders.Holder}) VALUES (@{Holders.LockName}, @{Holders.Holder})
       ON DUPLICATE KEY UPDATE {Holders.Holder} = @{Holders.Holder}
       """;

   static string NameOf(DbCommand command, string lockName) => DatabaseProcessLockKeys.Name(command.Connection!.Database, lockName);
}
