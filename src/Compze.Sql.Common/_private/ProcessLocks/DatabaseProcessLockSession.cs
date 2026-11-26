using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal.ProcessLocks;

namespace Compze.Sql.Common._private.ProcessLocks;

///<summary>A held lock of a database server's <see cref="IDatabaseProcessLocks"/> in the form of a database session of its own, open: the<br/>
/// servers' session-scoped locks live exactly as long as the session, so holding the lock is keeping this connection open, and disposing<br/>
/// it, or the process dying, which closes the connection, releases it.</summary>
///<remarks>A ping keeps the session alive. It defeats what reaps idle database sessions, such as cloud database gateways: a reaped session<br/>
/// would release the lock under a live, healthy holder. And it detects the session dying, as when the database becomes unreachable from<br/>
/// this process, which it reports through <c>onLostWhileHeld</c>: another process may then take the lock while this one still holds it, so<br/>
/// the report must be loud.</remarks>
///<remarks>Disposal releases the lock explicitly, with the release command the engine gives, before closing the connection, and returns only<br/>
/// once that has completed. Closing the connection would release the lock too, but only once the server noticed the socket close: a<br/>
/// release that returned while the server still held the lock would refuse the lock's next holder a lock that is free. The release is<br/>
/// skipped only when the session already died under a live holder: the lock went with it.</remarks>
sealed class DatabaseProcessLockSession : IDatabaseProcessLockHold
{
   ///<summary>Well inside every common window for reaping idle sessions (cloud database gateways commonly reap at several minutes), and the<br/>
   /// longest it takes to find the session dead. No correctness rides on it: a paused holder keeps its session, and so its lock.</summary>
   static readonly TimeSpan KeepaliveInterval = TimeSpan.FromSeconds(30);

   readonly DbConnection _session;
   readonly Action<DbCommand> _setUpTheRelease;
   readonly Action<Exception> _onLostWhileHeld;
   readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly Task _keepaliveLoop;
   bool _sessionDiedUnderALiveHolder;
   int _disposed;

   ///<summary>The hold of a lock that <paramref name="session"/> holds: <paramref name="setUpTheRelease"/> sets up the command that releases it.</summary>
   internal DatabaseProcessLockSession(DbConnection session, Action<DbCommand> setUpTheRelease, Action<Exception> onLostWhileHeld)
   {
      _session = session;
      _setUpTheRelease = setUpTheRelease;
      _onLostWhileHeld = onLostWhileHeld;
      _keepaliveLoop = TaskCE.RunAsync(KeepTheSessionAliveUntilReleasedAsync);
   }

   async Task KeepTheSessionAliveUntilReleasedAsync()
   {
      while(true)
      {
         if(await IsReleasedWithinTheKeepaliveIntervalAsync().caf())
            return;

         try
         {
            var ping = _session.CreateCommand();
            await using var _ = ping.caf();
            ping.CommandText = "SELECT 1";
            await ping.ExecuteScalarAsync().caf();
         }
#pragma warning disable CA1031 // Any failure means the session's health is unknown, so the lock must be presumed lost; the report is loud, never swallowed.
         catch(Exception sessionDeath)
#pragma warning restore CA1031
         {
            _sessionDiedUnderALiveHolder = true;
            _onLostWhileHeld(sessionDeath);
            return;
         }
      }

      //WaitAsync's timeout, unlike a Task.Delay raced with Task.WhenAny, stops its timer when the release comes first. Its TimeoutException is
      //suppressed: the interval passing is the normal case, which the loop answers with a ping.
      async Task<bool> IsReleasedWithinTheKeepaliveIntervalAsync()
      {
         await _released.Task.WaitAsync(KeepaliveInterval).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         return _released.Task.IsCompleted;
      }
   }

   public void Dispose()
   {
      if(!IsTheFirstDisposal()) return;
      _released.TrySetResult();
      //todo:urgent: blocks until a thread-pool task ends, so the disposal stalls while the pool is busy. The memory image state store, which
      //   disposes its lock's hold synchronously, becomes IAsyncDisposable with dev_docs/TODO/container-disposal-of-async-only-components.md
      //   (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // The hold's synchronous Dispose waits for its keepalive loop to end.
      _keepaliveLoop.Wait();
#pragma warning restore VSTHRD002
      //After the keepalive loop has stopped, so that the release has the session to itself.
      if(!_sessionDiedUnderALiveHolder)
      {
         using var release = _session.CreateCommand();
         _setUpTheRelease(release);
         release.ExecuteNonQuery();
      }

      _session.Dispose();
   }

   public async ValueTask DisposeAsync()
   {
      if(!IsTheFirstDisposal()) return;
      _released.TrySetResult();
      await _keepaliveLoop.caf();
      //After the keepalive loop has stopped, so that the release has the session to itself.
      if(!_sessionDiedUnderALiveHolder)
      {
         var release = _session.CreateCommand();
         await using(release.caf())
         {
            _setUpTheRelease(release);
            await release.ExecuteNonQueryAsync().caf();
         }
      }

      await _session.DisposeAsync().caf();
   }

   bool IsTheFirstDisposal() => Interlocked.Exchange(ref _disposed, 1) == 0;
}
