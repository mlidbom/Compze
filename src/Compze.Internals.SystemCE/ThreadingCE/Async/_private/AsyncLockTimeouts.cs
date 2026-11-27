using System.Diagnostics;
using Compze.Threading;

namespace Compze.Internals.SystemCE.ThreadingCE.Async._private;

///<summary>The takers of an async lock that timed out, each with its <see cref="AsyncLockTimeoutException"/>, waiting for the stack<br/>
/// trace of the holder as it releases the lock: what <see cref="IAsyncLockCE"/> and <see cref="IThrowOnReentrancyAsyncLock"/> report<br/>
/// of an in-memory deadlock.</summary>
sealed class AsyncLockTimeouts
{
   static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(10);

   readonly IMonitor _monitor = IMonitor.New();
   WaitTimeout _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
   IReadOnlyList<AsyncLockTimeoutException> _waitingForTheHoldersStackTrace = [];

   internal AsyncLockTimeouts(LockTimeout timeout) => Timeout = timeout;

   ///<summary>How long the lock's takers wait before they time out.</summary>
   internal LockTimeout Timeout { get; }

   internal void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

   ///<summary>The exception for a taker that has timed out, which gets the holder's stack trace as it releases the lock.</summary>
   internal AsyncLockTimeoutException TimedOut() => _monitor.Locked(() =>
   {
      var exception = new AsyncLockTimeoutException(Timeout, _stackTraceFetchTimeout);
      Interlocked.Exchange(ref _waitingForTheHoldersStackTrace, [.._waitingForTheHoldersStackTrace, exception]);
      return exception;
   });

   ///<summary>Gives the takers that timed out the stack trace of the holder, which is releasing the lock.</summary>
   internal void TheHolderReleases()
   {
      // ReSharper disable once InconsistentlySynchronizedField The holder releases at every use: only a taker that timed out is worth the monitor.
      if(Volatile.Read(ref _waitingForTheHoldersStackTrace).Count == 0) return;

      _monitor.Locked(() =>
      {
         var stackTrace = new StackTrace(fNeedFileInfo: true);
         foreach(var exception in _waitingForTheHoldersStackTrace)
         {
            exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
         }

         Interlocked.Exchange(ref _waitingForTheHoldersStackTrace, []);
      });
   }
}
