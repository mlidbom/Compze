using Compze.Tessaging._internal.SqlLayer;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Microsoft.Data.Sqlite;

namespace Compze.Tessaging.Sqlite._private;

///<summary>The sqlite engine's held endpoint process lock: an <see cref="IProcessLock"/>, standing in for the database-session locks the server<br/>
/// engines use (see <see cref="ITessagingSqlLayer.IEndpointCatalogSqlLayer.TryTakeProcessLockAsync"/>). Sqlite has no server sessions to scope a<br/>
/// lock to, so the lock is a file beside the database file, which every process that can open the database can reach, and the operating system<br/>
/// releases a dead process's lock, as the servers release a dead session's locks.</summary>
///<remarks>An in-memory database exists only in its own process, so its lock is in Compze's folder in the temp directory, named for the process too:<br/>
/// identical connection strings in different processes name different databases, which must never contend. So is the lock of a <c>file:</c> URI<br/>
/// data source, which isn't a path whose directory can be found, named for the URI as it is spelled.</remarks>
sealed class SqliteEndpointProcessLockHold : ITessagingSqlLayer.IEndpointProcessLockHold
{
   readonly IProcessLockHold _hold;

   SqliteEndpointProcessLockHold(IProcessLockHold hold) => _hold = hold;

   ///<summary>Takes the process lock of the endpoint <paramref name="endpointName"/> in the database <paramref name="connectionString"/> names, if no<br/>
   /// other holder holds it: null if one does.</summary>
   internal static SqliteEndpointProcessLockHold? TryTake(string connectionString, string endpointName) =>
      ProcessLockOf(connectionString, endpointName).TryTake($"running the endpoint {endpointName}", WaitTimeout.Zero) is {} hold
         ? new SqliteEndpointProcessLockHold(hold)
         : null;

   public ValueTask DisposeAsync()
   {
      _hold.Dispose();
      return ValueTask.CompletedTask;
   }

   static IProcessLock ProcessLockOf(string connectionString, string endpointName)
   {
      var builder = new SqliteConnectionStringBuilder(connectionString);
      if(IsInMemory(builder))
         return IProcessLock.New(ProcessLockKeys.Name($"{builder.DataSource}|process:{Environment.ProcessId}", endpointName), LockDirectoryForDataSourcesWithNoDirectory());

      if(builder.DataSource.StartsWith("file:", StringComparison.Ordinal))
         return IProcessLock.New(ProcessLockKeys.Name(builder.DataSource, endpointName), LockDirectoryForDataSourcesWithNoDirectory());

      //The full path, so that different spellings of the same file's path, relative and absolute, are one database with one lock.
      var databaseFile = new FileInfo(Path.GetFullPath(builder.DataSource));
      return IProcessLock.New(ProcessLockKeys.Name(databaseFile.FullName, endpointName), databaseFile.Directory!);

      static bool IsInMemory(SqliteConnectionStringBuilder builder) => builder.Mode == SqliteOpenMode.Memory || builder.DataSource == ":memory:";
   }

   static DirectoryInfo LockDirectoryForDataSourcesWithNoDirectory() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Compze", "EndpointProcessLocks"));
}
