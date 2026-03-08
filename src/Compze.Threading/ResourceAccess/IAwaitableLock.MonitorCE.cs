using System.Diagnostics;
using Compze.Threading.ResourceAccess.Exceptions;
using Compze.Threading.Utilities;

namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableLock
{
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations.
#pragma warning disable CS0618 // Type or member is obsolete
   private class LockCE : ILock, IAwaitableLock, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA1001
   {
      public IDisposable TakeLock(LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout);
      public IDisposable TakeReadLock(LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout);
      public IDisposable TakeUpdateLock(LockTimeout? timeout = null) => TakeLock(LockType.Update, timeout);

      public IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Read, waitTimeout, lockTimeout);

      public IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Update, waitTimeout, lockTimeout);

      public IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, LockType.Read, waitTimeout, lockTimeout);

      LockTimeout LockTimeout { get; }
      WaitTimeout WaitTimeout { get; }
      public long ContentionCount => _monitor.ContentionCount;

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      readonly ThinMonitorWrapper _monitor = new();
      readonly Lock _timeoutLock = new();

      readonly IDisposable _readLock;
      readonly IDisposable _updateLock;

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(1);

      WaitTimeout _stackTraceFetchTimeout;

      public LockCE(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         LockTimeout = lockTimeout ?? LockTimeout.Default;
         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
         _readLock = new LockDisposer(ReleaseLock);
         _updateLock = new LockDisposer(() =>
         {
            _monitor.NotifyWaitingThreadsAboutUpdates(); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
            ReleaseLock();
         });
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      IDisposable LockFor(LockType lockType)
      {
         return lockType switch
         {
            LockType.Read   => _readLock,
            LockType.Update => _updateLock,
            _               => throw new ArgumentOutOfRangeException(nameof(lockType), lockType, message: null)
         };
      }

      IDisposable TakeLock(LockType lockType, LockTimeout? lockTimeout = null) => TryTakeLock(lockType, lockTimeout) ?? throw RegisterTimeoutException();

      IDisposable TakeLockWhen(Func<bool> condition, LockType lockType, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, lockType, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      IDisposable? TryTakeLock(LockType lockType, LockTimeout? timeout = null) => _monitor.TryTakeLock(timeout ?? LockTimeout) ? LockFor(lockType) : null;

      IDisposable? TryTakeLockWhen(Func<bool> condition, LockType lockType, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         IDisposable takenLock = TakeLock(lockType, effectiveLockTimeout);
         try
         {
            if(effectiveWaitTimeout.IsInfinite)
            {
               while(!condition())
                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout.Infinite);
            } else
            {
               while(!condition())
               {
                  if(effectiveWaitTimeout.IsExpired(waitStartedAt))
                  {
                     takenLock.Dispose();
                     return null;
                  }

                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(effectiveWaitTimeout.TimeRemaining(waitStartedAt));
               }
            }
         }
         catch
         {
            ReleaseLock();
            throw;
         }

         return LockFor(lockType);
      }

      void ReleaseLock()
      {
         UpdateAnyRegisteredTimeoutExceptions();
         _monitor.ReleaseLock();
      }

      IReadOnlyList<TakeLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<TakeLockTimeoutException>();

      Exception RegisterTimeoutException()
      {
         lock(_timeoutLock)
         {
            var exception = new TakeLockTimeoutException(LockTimeout, _stackTraceFetchTimeout);
            OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _timeOutExceptionsOnOtherThreads, exception);
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

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<TakeLockTimeoutException>());
            }
         }
      }

      enum LockType
      {
         Read = 0,
         Update = 1
      }

      class ThinMonitorWrapper
      {
         readonly object _lockObject = new();
         long _contentionCount = 0;
         public long ContentionCount => _contentionCount;

         public bool TryTakeLock(LockTimeout timeout)
         {
            if(Monitor.TryEnter(_lockObject)) return true; //This will never block, calling it is essentially free and allows us to collect contention statistics
            Interlocked.Increment(ref _contentionCount);
            return Monitor.TryEnter(_lockObject, timeout);
         }

         public void ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout timeout) => Monitor.Wait(_lockObject, timeout);

         public void ReleaseLock() => Monitor.Exit(_lockObject);

         public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
      }
   }
}
