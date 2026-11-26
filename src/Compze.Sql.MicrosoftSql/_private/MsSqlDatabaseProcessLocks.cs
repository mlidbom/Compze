using System.Data.Common;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.MicrosoftSql._internal;
using Microsoft.Data.SqlClient;
using Holders = Compze.Sql.Common._internal.ProcessLocks.DatabaseProcessLockHolderSchemaStrings;

namespace Compze.Sql.MicrosoftSql._private;

///<summary>The process locks of a SQL Server database: each an application lock (<c>sp_getapplock</c>) owned by the session that takes it. An<br/>
/// application lock's resource is scoped to the database it is taken in, so the lock's name alone names it.</summary>
sealed class MsSqlDatabaseProcessLocks : DatabaseServerProcessLocks
{
   readonly IMsSqlConnectionPool _connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager;

   internal MsSqlDatabaseProcessLocks(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

   internal static readonly string SchemaCreationSql =
      $"""

       IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Holders.TableName}')
           CREATE TABLE {Holders.TableName}
           (
               {Holders.LockName} nvarchar({Holders.LockNameLength}) NOT NULL CONSTRAINT PK_{Holders.TableName} PRIMARY KEY CLUSTERED,
               {Holders.Holder}   nvarchar({Holders.HolderLength})   NOT NULL
           );

       """;

   protected override DbConnection NewSessionOfItsOwn() =>
      new SqlConnection(new SqlConnectionStringBuilder(_connectionPool.ConnectionString) { Pooling = false, Enlist = false }.ConnectionString);

   protected override void EnsureTheHolderTableExists() => _schemaManager.EnsureSchemaInitialized();

   protected override void SetUpTheTry(DbCommand command, string lockName)
   {
      command.CommandText = """
                            DECLARE @LockResult int;
                            EXEC @LockResult = sp_getapplock @Resource = @LockResource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;
                            SELECT CASE WHEN @LockResult >= 0 THEN 1 WHEN @LockResult = -1 THEN 0 ELSE @LockResult END;
                            """;
      AddParameter(command, "LockResource", ResourceOf(lockName));
   }

   protected override void SetUpTheRelease(DbCommand command, string lockName)
   {
      command.CommandText = "EXEC sp_releaseapplock @Resource = @LockResource, @LockOwner = 'Session';";
      AddParameter(command, "LockResource", ResourceOf(lockName));
   }

   protected override string RecordTheHolderSql =>
      $"""
       UPDATE {Holders.TableName} SET {Holders.Holder} = @{Holders.Holder} WHERE {Holders.LockName} = @{Holders.LockName};
       IF @@ROWCOUNT = 0
          INSERT INTO {Holders.TableName} ({Holders.LockName}, {Holders.Holder}) VALUES (@{Holders.LockName}, @{Holders.Holder});
       """;

   static string ResourceOf(string lockName) => $"Compze.DatabaseProcessLock:{lockName}";
}
