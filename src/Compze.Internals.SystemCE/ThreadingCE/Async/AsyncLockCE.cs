using System.Diagnostics;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using Compze.Threading;
using Compze.Internals.SystemCE._private;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

///<summary>A lock that may be held across awaits, which synchronous and asynchronous code take alike. It is re-entrant per async<br/>
/// flow, so a nested use on the same flow doesn't deadlock, while independent flows take turns. Taking it times out after its<br/>
/// <see cref="LockTimeout"/>.</summary>
///<remarks>It isn't disposable: nothing it holds needs disposing, and disposing a lock that is held, or waited for, would break<br/>
/// its holder's release of it and its waiters.</remarks>
public interface IAsyncLockCE
{
   static IAsyncLockCE WithDefaultTimeout() => new AsyncLockCE(LockTimeout.Default);
   static IAsyncLockCE New(LockTimeout timeout) => new AsyncLockCE(timeout);

   Task<Unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   Unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);

   void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);


#pragma warning disable CA1001 // The semaphore holds nothing to dispose: SemaphoreSlim allocates only its AvailableWaitHandle, which this never asks for. See IAsyncLockCE's remarks.
   public class AsyncLockCE : IAsyncLockCE
#pragma warning restore CA1001
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      readonly AsyncLocal<int> _lockEntranceCount = new();
      //PROBE: which hold this flow re-enters on, and the hold that has the semaphore now.
      readonly AsyncLocal<ProbeHold?> _probeHoldOfThisFlow = new();
      volatile ProbeHold? _probeCurrentHold;
      sealed class ProbeHold(string acquiredAt) { public readonly string AcquiredAt = acquiredAt; }

      void ProbeAcquired()
      {
         var hold = new ProbeHold(Environment.StackTrace);
         _probeCurrentHold = hold;
         _probeHoldOfThisFlow.Value = hold;
      }

      void ProbeReEntering()
      {
         var hold = _probeHoldOfThisFlow.Value;
         if(ReferenceEquals(hold, _probeCurrentHold)) return;
         var message = $"""
                        PROBE AsyncLockCE: a flow re-enters on a hold that has ended, while {(_probeCurrentHold == null ? "no one" : "another flow")} holds the lock.
                        ==== The ended hold was acquired at:
                        {hold?.AcquiredAt ?? "(no hold recorded)"}
                        ==== The re-entry is at:
                        {Environment.StackTrace}
                        """;
         Compze.Internals.SystemCE._private.ProbeLog.Write(message);
         throw new InvalidOperationException(message);
      }
      readonly LockTimeout _timeout;
      readonly Lock _timeoutLock = new();

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(10);

      WaitTimeout _stackTraceFetchTimeout;
      IReadOnlyList<AsyncLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AsyncLockTimeoutException>();

      internal AsyncLockCE(LockTimeout timeout)
      {
         _timeout = timeout;
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public async Task<Unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.ToAsyncFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         if(_lockEntranceCount.Value == 0)
         {
            if(!await _semaphore.WaitAsync(_timeout).caf())
            {
               throw RegisterTimeoutException();
            }
            ProbeAcquired();
         } else ProbeReEntering();

         _lockEntranceCount.Value += 1;
         await using var exit = new AsyncDisposable(Exit).ConfigureAwait(false);//Analyzer does not understand caf() here for some reason and emits a warning.
         return await lockedAction().caf();
      }

      public Unit Locked(Action lockedAction) => Locked(lockedAction.ToFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         if(_lockEntranceCount.Value == 0)
         {
            if(!_semaphore.Wait(_timeout))
            {
               throw RegisterTimeoutException();
            }
            ProbeAcquired();
         } else ProbeReEntering();

         _lockEntranceCount.Value += 1;
         using var exit = new Disposable(Exit);
         return lockedAction();
      }

      void Exit()
      {
         _lockEntranceCount.Value -= 1;
         if(_lockEntranceCount.Value == 0)
         {
            UpdateAnyRegisteredTimeoutExceptions();
            _probeCurrentHold = null;
            _semaphore.Release();
         }
      }

      Exception RegisterTimeoutException()
      {
         //refactor: _timeoutLock is a raw lock, here and in UpdateAnyRegisteredTimeoutExceptions, where the locking standard asks for
         //   Compze.Threading's primitives, such as IMonitor, which this package already builds on.
         lock(_timeoutLock)
         {
            var exception = new AsyncLockTimeoutException(_timeout, _stackTraceFetchTimeout);
            Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, [.._timeOutExceptionsOnOtherThreads, exception]);
            return exception;
         }
      }

      void UpdateAnyRegisteredTimeoutExceptions()
      {
         // ReSharper disable once InconsistentlySynchronizedField
         if(_timeOutExceptionsOnOtherThreads.Count > 0)
         {
            lock(_timeoutLock)
            {
               var stackTrace = new StackTrace(fNeedFileInfo: true);
               foreach(var exception in _timeOutExceptionsOnOtherThreads)
               {
                  exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
               }

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<AsyncLockTimeoutException>());
            }
         }
      }
   }
}
