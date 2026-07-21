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
class ProcessLockSession : ITessagingSqlLayer.IEndpointProcessLockHold
{
   ///<summary>Well inside every common idle-session-reaping window (cloud database gateways commonly reap at several<br/>
   /// minutes), and the worst-case latency for detecting the session dying. Detection latency only — no correctness rides<br/>
   /// on this value: a paused holder keeps its session and therefore its lock.</summary>
   static readonly TimeSpan KeepaliveInterval = TimeSpan.FromSeconds(30);

   readonly DbConnection _connection;
   readonly Action<Exception> _onLockLostWhileHeld;
   readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly Task _keepaliveLoop;

   internal ProcessLockSession(DbConnection connection, Action<Exception> onLockLostWhileHeld)
   {
      _connection = connection;
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
            _onLockLostWhileHeld(sessionDeath);
            return;
         }
      }
   }

   public async ValueTask DisposeAsync()
   {
      _released.TrySetResult();
      await _keepaliveLoop.caf();
      //Closing the session is what releases the server-side session-scoped lock.
      await _connection.DisposeAsync().caf();
   }
}
