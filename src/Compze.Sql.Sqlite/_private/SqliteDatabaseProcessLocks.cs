using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.Sqlite._internal;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite._private;

///<summary>The process locks of a SQLite database: each an <see cref="IProcessLock"/>, standing in for the session-scoped locks of the database<br/>
/// servers, since SQLite has no sessions to scope a lock to. The lock is a file beside the database's file, which every process that can<br/>
/// open the database can reach, and the operating system releases a dead process's lock, as the servers release a dead session's.</summary>
///<remarks>An in-memory database exists only in its own process, so its locks are in <see cref="LockDirectoryForDataSourcesWithNoDirectory"/>, named<br/>
/// for the process too: identical connection strings in different processes name different databases, which must never contend. So are the<br/>
/// locks of a <c>file:</c> URI data source, which isn't a path whose directory can be found, named for the URI as it is spelled.</remarks>
///<remarks>The holder records itself in the lock's own file, as every <see cref="IProcessLock"/> does, rather than in a table: a write to the<br/>
/// database would wait for SQLite's write lock, which a unit of work that takes a lock, as a memory image state store's first use may, could<br/>
/// hold on the same database already. A live holder can't lose a file it holds open, so <c>onLostWhileHeld</c> is never called.</remarks>
sealed class SqliteDatabaseProcessLocks : IDatabaseProcessLocks
{
   readonly ISqliteConnectionPool _connectionPool;

   internal SqliteDatabaseProcessLocks(ISqliteConnectionPool connectionPool) => _connectionPool = connectionPool;

   public IDatabaseProcessLockHold? TryTake(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      return ProcessLockNamed(lockName).TryTake(purpose, waitTimeout) is {} hold ? new Hold(hold) : null;
   }

   public async Task<IDatabaseProcessLockHold?> TryTakeAsync(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld)
   {
      DatabaseProcessLockHolder.AssertIsALockName(lockName);
      return await ProcessLockNamed(lockName).TryTakeAsync(purpose, waitTimeout).caf() is {} hold ? new Hold(hold) : null;
   }

   public string? HolderOf(string lockName) => ProcessLockNamed(lockName).LastHoldersDescription();

   public Task<string?> HolderOfAsync(string lockName) => Task.FromResult(HolderOf(lockName));

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
