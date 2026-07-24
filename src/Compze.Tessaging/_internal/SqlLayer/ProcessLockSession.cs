using System.Data.Common;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging._internal.SqlLayer;

///<summary>A held endpoint process lock in the form of a dedicated, open database session (see<br/>
/// <see cref="ITessagingSqlLayer.IEndpointCatalogSqlLayer.TryTakeProcessLockAsync"/>): the server engines' session-scoped<br/>
/// locks live exactly as long as the session, so holding the lock IS keeping this connection open, and disposing —<br/>
/// or the process dying, which closes the connection — releases it.</summary>
///<remarks>The session is kept alive by a periodic ping. The ping serves two purposes: it defeats infrastructure that reaps<br/>
/// idle database sessions (a reaped session would silently release the lock under a live, healthy holder), and it detects<br/>
/// the session dying — the domain database unreachable from this process — which it reports through<br/>
/// <c>onLockLostWhileHeld</c>: after that, another process may legitimately claim the endpoint while this process still<br/>
/// runs it, so the report must be loud.</remarks>
///<remarks>Disposal releases the lock explicitly — the release command the engine supplies at construction — before closing<br/>
/// the connection, and returns only once that has completed. Closing the connection would release the lock too, but only once<br/>
/// the server got round to noticing the socket close: a release that returned while the server still held the lock would let<br/>
/// the endpoint's next process be refused a lock that is logically free. The explicit release is skipped only when the<br/>
/// session already died under a live holder — the lock went with it, and there is nothing to release on a dead connection.</remarks>
class ProcessLockSession : ITessagingSqlLayer.IEndpointProcessLockHold
{
   ///<summary>Well inside every common idle-session-reaping window (cloud database gateways commonly reap at several<br/>
   /// minutes), and the worst-case latency for detecting the session dying. Detection latency only — no correctness rides<br/>
   /// on this value: a paused holder keeps its session and therefore its lock.</summary>
   static readonly TimeSpan KeepaliveInterval = TimeSpan.FromSeconds(30);

   readonly DbConnection _connection;
   readonly Func<DbConnection, Task> _releaseLockOnSessionAsync;
   readonly Action<Exception> _onLockLostWhileHeld;
   readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly Task _keepaliveLoop;
   bool _sessionDiedUnderLiveHolder;

   internal ProcessLockSession(DbConnection connection, Func<DbConnection, Task> releaseLockOnSessionAsync, Action<Exception> onLockLostWhileHeld)
   {
      _connection = connection;
      _releaseLockOnSessionAsync = releaseLockOnSessionAsync;
      _onLockLostWhileHeld = onLockLostWhileHeld;
      _keepaliveLoop = TaskCE.Run(KeepSessionAliveUntilReleasedAsync);
   }

   async Task KeepSessionAliveUntilReleasedAsync()
   {
      while(true)
      {
         if(await Task.WhenAny(_released.Task, Task.Delay(KeepaliveInterval)).caf() == _released.Task)
            return;

         try
         {
            var ping = _connection.CreateCommand();
            await using var _ = ping.caf();
            ping.CommandText = "SELECT 1";
            await ping.ExecuteScalarAsync().caf();
         }
#pragma warning disable CA1031 // Any failure means the session's health is unknown, so the lock must be presumed lost; the report is loud, never swallowed.
         catch(Exception sessionDeath)
#pragma warning restore CA1031
         {
            _sessionDiedUnderLiveHolder = true;
            _onLockLostWhileHeld(sessionDeath);
            return;
         }
      }
   }

   public async ValueTask DisposeAsync()
   {
      _released.TrySetResult();
      await _keepaliveLoop.caf();
      //Awaited after the keepalive loop has stopped, so the release command has the connection to itself. A dead session
      //already released the lock, so there is nothing to release and the command would only throw on the dead connection.
      if(!_sessionDiedUnderLiveHolder) await _releaseLockOnSessionAsync(_connection).caf();
      await _connection.DisposeAsync().caf();
   }
}
