using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Compze.Contracts;
using Compze.Threading.ResourceAccess.Exceptions;
using Compze.Threading.Utilities;

namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableMonitor
{
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations.
   private class MonitorCE : IMonitor, IAwaitableMonitor, IMonitorInternals
#pragma warning restore CA1001
   {
      public IDisposable TakeLock(LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout);
      public IDisposable TakeReadLock(LockTimeout? timeout = null) => TakeLock(LockType.Read, timeout);
      public IDisposable TakeUpdateLock(LockTimeout? timeout = null) => TakeLock(LockType.Update, timeout);

      public IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Read, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

      public IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, LockType.Update, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

      public IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, LockType.Read, throwOnFailedLock: false, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

      public IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, LockType.Update, throwOnFailedLock: false, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

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

      public MonitorCE(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
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
         TryTakeLockWhen(condition, lockType, throwOnFailedLock: true, waitTimeout: waitTimeout, lockTimeout: lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      IDisposable? TryTakeLock(LockType lockType, LockTimeout? timeout = null) => _monitor.TryTakeLock(timeout ?? LockTimeout) ? LockFor(lockType) : null;

      IDisposable? TryTakeLockWhen(Func<bool> condition, LockType lockType, bool throwOnFailedLock, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         IDisposable takenLock;
         if(throwOnFailedLock)
         {
            takenLock = TakeLock(lockType, effectiveLockTimeout);
         } else
         {
            var attemptedLock = TryTakeLock(lockType, effectiveLockTimeout);
            if(attemptedLock == null) return null;
            takenLock = attemptedLock;
         }

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
         static readonly TimeSpan InfiniteTimeOut = -1.Milliseconds(); //https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor.tryenter?view=net-9.0
         readonly object _lockObject = new();
         long _contentionCount = 0;
         public long ContentionCount => _contentionCount;

         public bool TryTakeLock(LockTimeout timeout)
         {
            if(Monitor.TryEnter(_lockObject)) return true; //This will never block, calling it is essentially free and allows us to collect contention statistics
            Interlocked.Increment(ref _contentionCount);
            return Monitor.TryEnter(_lockObject, timeout.Value);
         }

         public void ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout timeout) => Monitor.Wait(_lockObject, timeout.Value);

         public void ReleaseLock() => Monitor.Exit(_lockObject);

         public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
      }
   }
}
