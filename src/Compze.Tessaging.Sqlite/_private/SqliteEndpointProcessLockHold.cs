using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Microsoft.Data.Sqlite;

namespace Compze.Tessaging.Sqlite._private;

///<summary>The sqlite engine's endpoint process lock: an OS-level machine-wide mutex standing in for the database-session<br/>
/// locks the server engines use (see <see cref="ITessagingSqlLayer.IEndpointCatalogSqlLayer.TryTakeProcessLockAsync"/>) —<br/>
/// sqlite has no server sessions to scope a lock to, but a sqlite database is machine-local by nature, so a machine-wide<br/>
/// OS lock covers every process that could open it. The OS releases a dead process's mutex (abandonment), giving the<br/>
/// same crash-release the server engines get from their sessions dying.</summary>
///<remarks>An OS mutex is thread-affine: only the thread that acquired it may release it. The hold therefore runs a<br/>
/// dedicated parked thread that acquires, waits for the release request, and releases — disposal signals it and awaits<br/>
/// its exit.</remarks>
class SqliteEndpointProcessLockHold : ITessagingSqlLayer.IEndpointProcessLockHold
{
   readonly IMutex _mutex;
   readonly TaskCompletionSource<bool> _acquisitionResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly TaskCompletionSource _releaseRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly TaskCompletionSource _holderThreadExited = new(TaskCreationOptions.RunContinuationsAsynchronously);

   internal static async Task<SqliteEndpointProcessLockHold?> TryTakeAsync(string connectionString, string endpointName)
   {
      var hold = new SqliteEndpointProcessLockHold(connectionString, endpointName);
      if(await hold._acquisitionResult.Task.caf()) return hold;

      await hold.DisposeAsync().caf();
      return null;
   }

   SqliteEndpointProcessLockHold(string connectionString, string endpointName)
   {
      _mutex = IMutex.Global(MutexName(connectionString, endpointName));
      new Thread(HoldUntilReleaseRequested) { IsBackground = true, Name = $"EndpointProcessLockHolder:{endpointName}" }.Start();
   }

   void HoldUntilReleaseRequested()
   {
      var mutexLock = _mutex.TryTakeLock(LockTimeout.Zero);
      _acquisitionResult.SetResult(mutexLock != null);
      if(mutexLock != null)
      {
         _releaseRequested.Task.GetAwaiter().GetResult();
         mutexLock.Dispose();
      }

      _holderThreadExited.SetResult();
   }

   public async ValueTask DisposeAsync()
   {
      _releaseRequested.TrySetResult();
      await _holderThreadExited.Task.caf();
      _mutex.Dispose();
   }

   static string MutexName(string connectionString, string endpointName)
   {
      var builder = new SqliteConnectionStringBuilder(connectionString);
      //A Mode=Memory database exists only within its own process, so the process id joins the identity: identical
      //connection strings in different processes name different databases and must never contend.
      var databaseIdentity = IsInMemory(builder)
                                ? $"{builder.DataSource}|process:{Environment.ProcessId}"
                                : BestEffortCanonicalPath(builder.DataSource);
      return ProcessLockKeys.Name(databaseIdentity, endpointName);

      static bool IsInMemory(SqliteConnectionStringBuilder builder) => builder.Mode == SqliteOpenMode.Memory || builder.DataSource == ":memory:";

      //Different spellings of the same file path (relative vs absolute) must contend as one database; file: URIs are
      //left as spelled - they are not paths GetFullPath understands.
      static string BestEffortCanonicalPath(string dataSource) => dataSource.StartsWith("file:", StringComparison.Ordinal) ? dataSource : Path.GetFullPath(dataSource);
   }
}
