using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Threading.Exceptions;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.TestInfrastructure;

///<summary>The deadlock the lock-timeout specifications assert against, set up deterministically: one thread takes the lock
/// and holds it — parked on a gate, not a timed sleep — until this scenario is released or disposed, while a second thread
/// times out trying to take it.</summary>
///<remarks>Because the lock is genuinely held for the whole window, the second thread's acquisition times out at any timer
/// resolution, with no race to lose. It replaces the earlier "the holder sleeps X, the competitor times out at Y &lt; X, hope
/// X &gt; Y wins the footrace" design, which flaked under Windows' ~15 ms timer granularity where X and Y round together.</remarks>
sealed class LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt : IDisposable
{
   readonly ManualResetEvent _releaseTheHeldLock = new(false);
   readonly Task _holder;

   ///<summary>The exception the second thread got when its acquisition timed out against the held lock.</summary>
   internal Exception TheAcquisitionTimeoutException { get; }

   ///<param name="lockDiagnostics">The lock under specification, exposing its stack-trace-fetch timeout.</param>
   ///<param name="takeLock">Takes the lock under specification (<c>TakeLock</c> / <c>TakeUpdateLock</c> — this scenario is agnostic to which).</param>
   ///<param name="stackTraceFetchTimeout">How long a timeout exception waits to capture the holder's disposal stack trace. Left at the lock's default when null.</param>
   internal LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt(ILockInternals lockDiagnostics, Func<IDisposable> takeLock, WaitTimeout? stackTraceFetchTimeout = null)
   {
      if(stackTraceFetchTimeout.HasValue) lockDiagnostics.SetTimeToWaitForStackTrace(stackTraceFetchTimeout.Value);

      using var theLockIsHeld = new ManualResetEvent(false);
      _holder = TaskCE.Run(() =>
      {
         var heldLock = takeLock();
         theLockIsHeld.Set();
         _releaseTheHeldLock.WaitOne();
         DisposeInMethodSoItWillBeInTheCapturedCallStack(heldLock);
      });
      theLockIsHeld.WaitOne();

      //The lock is genuinely held and is not released until we say so, so this acquisition MUST time out. No race: the
      //timeout fires against a lock that is provably unavailable for its entire duration, whatever the timer resolution.
      TheAcquisitionTimeoutException = Invoking(() => TaskCE.Run(takeLock).Wait())
                                      .Must().Throw<AggregateException>()
                                      .Which.InnerExceptions.Single();
   }

   ///<summary>Releases the held lock and waits for the holder to finish disposing it. The disposal, running inside
   /// <see cref="DisposeInMethodSoItWillBeInTheCapturedCallStack"/>, populates the timeout exception's captured stack trace, so
   /// after this returns the caller can read <see cref="TheAcquisitionTimeoutException"/>'s message with the stack trace in place.</summary>
   internal void ReleaseTheHeldLock()
   {
      _releaseTheHeldLock.Set();
      _holder.Wait();
   }

   ///<summary>The held lock is disposed from inside this named method so the method's name is on the call stack the timeout
   /// exception captures at disposal — which is exactly what the "message contains the holder's stack trace" specification looks for.</summary>
   internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

   public void Dispose()
   {
      _releaseTheHeldLock.Set();
      _holder.Wait();
      _releaseTheHeldLock.Dispose();
   }
}
