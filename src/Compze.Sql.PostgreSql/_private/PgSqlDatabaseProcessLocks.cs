using System.Data.Common;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.PostgreSql._internal;
using Npgsql;
using Holders = Compze.Sql.Common._internal.ProcessLocks.DatabaseProcessLockHolderSchemaStrings;

namespace Compze.Sql.PostgreSql._private;

///<summary>The process locks of a PostgreSQL database: each a session-scoped advisory lock, keyed by a 64-bit integer. The database's name<br/>
/// is part of what the key is hashed from, so that locks of the same name in two databases of one server never collide, however the<br/>
/// server scopes advisory locks.</summary>
sealed class PgSqlDatabaseProcessLocks : DatabaseServerProcessLocks
{
   internal static readonly IReadOnlyList<string> SchemaCreationStatements =
   [
      $"""
       CREATE TABLE IF NOT EXISTS {Holders.TableName}
       (
         {Holders.LockName} TEXT NOT NULL,
         {Holders.Holder}   TEXT NOT NULL,

         PRIMARY KEY ( {Holders.LockName} )
       )
       """
   ];

   readonly IPgSqlConnectionPool _connectionPool;
   readonly PgSqlSqlLayerSchemaManager _schemaManager;

   internal PgSqlDatabaseProcessLocks(IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

   protected override DbConnection NewSessionOfItsOwn() =>
      new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_connectionPool.ConnectionString) { Pooling = false, Enlist = false }.ConnectionString);

   protected override void EnsureTheHolderTableExists() => _schemaManager.EnsureSchemaInitialized();

   protected override void SetUpTheTry(DbCommand command, string lockName)
   {
      command.CommandText = "SELECT CASE WHEN pg_try_advisory_lock(@LockKey) THEN 1 ELSE 0 END";
      AddParameter(command, "LockKey", KeyOf(command, lockName));
   }

   protected override void SetUpTheRelease(DbCommand command, string lockName)
   {
      command.CommandText = "SELECT pg_advisory_unlock(@LockKey)";
      AddParameter(command, "LockKey", KeyOf(command, lockName));
   }

   protected override string RecordTheHolderSql =>
      $"""
       INSERT INTO {Holders.TableName} ({Holders.LockName}, {Holders.Holder}) VALUES (@{Holders.LockName}, @{Holders.Holder})
       ON CONFLICT ({Holders.LockName}) DO UPDATE SET {Holders.Holder} = EXCLUDED.{Holders.Holder}
       """;

   static long KeyOf(DbCommand command, string lockName) => DatabaseProcessLockKeys.Int64Key(command.Connection!.Database, lockName);
}
