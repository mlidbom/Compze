using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.Sqlite._internal;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Microsoft.Data.Sqlite;
using Holders = Compze.Sql.Common._internal.ProcessLocks.DatabaseProcessLockHolderSchemaStrings;

namespace Compze.Sql.Sqlite._private;

///<summary>The process locks of a SQLite database: each an <see cref="IProcessLock"/>, standing in for the session-scoped locks of the database<br/>
/// servers, since SQLite has no sessions to scope a lock to. The lock is a file beside the database's file, which every process that can<br/>
/// open the database can reach, and the operating system releases a dead process's lock, as the servers release a dead session's.</summary>
///<remarks>An in-memory database exists only in its own process, so its locks are in <see cref="LockDirectoryForDataSourcesWithNoDirectory"/>, named<br/>
/// for the process too: identical connection strings in different processes name different databases, which must never contend. So are the<br/>
/// locks of a <c>file:</c> URI data source, which isn't a path whose directory can be found, named for the URI as it is spelled.</remarks>
///<remarks>A live holder can't lose a file it holds open, so <c>onLostWhileHeld</c> is never called. Since the lock isn't a database session, the<br/>
/// holder record can't be written on it: it is the next thing written once the lock is held, before the hold is handed out.</remarks>
sealed class SqliteDatabaseProcessLocks : IDatabaseProcessLocks
{
   internal const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Holders.TableName}
       (
           {Holders.LockName} TEXT NOT NULL,
           {Holders.Holder}   TEXT NOT NULL,

           PRIMARY KEY ( {Holders.LockName} )
       );

       """;

   readonly ISqliteConnectionPool _connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager;

   internal SqliteDatabaseProcessLocks(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

   public IDatabaseProcessLockHold? TryTake(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      var holder = DatabaseProcessLockHolder.ThisProcessTakingItFor(purpose);
      _schemaManager.EnsureSchemaInitialized();
      if(ProcessLockNamed(lockName).TryTake(purpose, waitTimeout) is not {} hold) return null;
      try
      {
         TransactionScopeCe.SuppressAmbient(() => _connectionPool.UseCommand(command => RecordTheHolder(command, lockName, holder).ExecuteNonQuery()));
         return new Hold(hold);
      }
      catch //Releasing what we took, not handling: a record that fails must not leave the lock held with no hold to release it.
      {
         hold.Dispose();
         throw;
      }
   }

   public async Task<IDatabaseProcessLockHold?> TryTakeAsync(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      var holder = DatabaseProcessLockHolder.ThisProcessTakingItFor(purpose);
      _schemaManager.EnsureSchemaInitialized();
      if(await ProcessLockNamed(lockName).TryTakeAsync(purpose, waitTimeout).caf() is not {} hold) return null;
      try
      {
         await TransactionScopeCe.SuppressAmbientAsync(async () => await _connectionPool.UseCommandAsync(async command => await RecordTheHolder(command, lockName, holder).ExecuteNonQueryAsync().caf()).caf()).caf();
         return new Hold(hold);
      }
      catch //Releasing what we took, not handling: a record that fails must not leave the lock held with no hold to release it.
      {
         hold.Dispose();
         throw;
      }
   }

   public string? HolderOf(string lockName)
   {
      _schemaManager.EnsureSchemaInitialized();
      return TransactionScopeCe.Execute(() => _connectionPool.UseCommand(command => ReadTheHolder(command, lockName).ExecuteScalar() as string), System.Transactions.TransactionScopeOption.Suppress);
   }

   public async Task<string?> HolderOfAsync(string lockName)
   {
      _schemaManager.EnsureSchemaInitialized();
      return await TransactionScopeCe.SuppressAmbientAsync(async () => await _connectionPool.UseCommandAsync(async command => await ReadTheHolder(command, lockName).ExecuteScalarAsync().caf() as string).caf()).caf();
   }

   static SqliteCommand RecordTheHolder(SqliteCommand command, string lockName, string holder) =>
      command.SetCommandText($"INSERT OR REPLACE INTO {Holders.TableName} ({Holders.LockName}, {Holders.Holder}) VALUES (@{Holders.LockName}, @{Holders.Holder})")
             .AddMediumTextParameter(Holders.LockName, lockName)
             .AddMediumTextParameter(Holders.Holder, holder);

   static SqliteCommand ReadTheHolder(SqliteCommand command, string lockName) =>
      command.SetCommandText($"SELECT {Holders.Holder} FROM {Holders.TableName} WHERE {Holders.LockName} = @{Holders.LockName}")
             .AddMediumTextParameter(Holders.LockName, lockName);

   IProcessLock ProcessLockNamed(string lockName)
   {
      var builder = new SqliteConnectionStringBuilder(_connectionPool.ConnectionString);
      if(IsInMemory(builder))
         return IProcessLock.New(DatabaseProcessLockKeys.Name($"{builder.DataSource}|process:{Environment.ProcessId}", lockName), LockDirectoryForDataSourcesWithNoDirectory());

      if(builder.DataSource.StartsWith("file:", StringComparison.Ordinal))
         return IProcessLock.New(DatabaseProcessLockKeys.Name(builder.DataSource, lockName), LockDirectoryForDataSourcesWithNoDirectory());

      var databaseFile = TheDatabaseFileByItsFullPath(builder);
      return IProcessLock.New(DatabaseProcessLockKeys.Name(databaseFile.FullName, lockName), databaseFile.Directory._assert().NotNull());

      //By its full path, so that different spellings of the file's path, relative and absolute, are one database with one lock.
      static FileInfo TheDatabaseFileByItsFullPath(SqliteConnectionStringBuilder builder) => new(Path.GetFullPath(builder.DataSource));
      static bool IsInMemory(SqliteConnectionStringBuilder builder) => builder.Mode == SqliteOpenMode.Memory || builder.DataSource == ":memory:";
   }

   ///<summary><c>Compze/DatabaseProcessLocks</c> in the temp directory: where the locks of a data source that has no directory of its own are.</summary>
   static DirectoryInfo LockDirectoryForDataSourcesWithNoDirectory() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Compze", "DatabaseProcessLocks"));

   ///<summary>A held lock: the held <see cref="IProcessLock"/>.</summary>
   sealed class Hold(IProcessLockHold hold) : IDatabaseProcessLockHold
   {
      readonly IProcessLockHold _hold = hold;

      public void Dispose() => _hold.Dispose();

      public ValueTask DisposeAsync()
      {
         _hold.Dispose();
         return ValueTask.CompletedTask;
      }
   }
}
