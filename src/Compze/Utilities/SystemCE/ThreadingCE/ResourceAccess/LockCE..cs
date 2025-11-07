using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible. They are not normal disposables
///<summary>The monitor class exposes a rather obscure, brittle and easily misused API in my opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
class MonitorCE : IMonitorCE
#pragma warning restore CA1001
{
   public TimeSpan LockTimeout { get; }
   public TimeSpan WaitTimeout { get; }

   public IDisposable TakeReadLock(TimeSpan? timeout = null) => TakeLock(LockType.Read, timeout ?? LockTimeout);
   public IDisposable TakeUpdateLock(TimeSpan? timeout = null) => TakeLock(LockType.Update, timeout ?? LockTimeout);

   public IDisposable TakeReadLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) => TakeLockWhen(condition, LockType.Read, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
   public IDisposable TakeUpdateLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) => TakeLockWhen(condition, LockType.Update, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   public IDisposable? TryTakeReadLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) => TryTakeLockWhen(condition, LockType.Read, throwOnFailedLock: false, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
   public IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) => TryTakeLockWhen(condition, LockType.Update, throwOnFailedLock: false, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

   readonly ThinMonitorWrapper _monitor = new();
   readonly Lock _timeoutLock = new();

   readonly IDisposable _readLock;
   readonly IDisposable _updateLock;

   static readonly TimeSpan DefaultTimeToWaitForStackTrace = 1.Seconds();

   TimeSpan _stackTraceFetchTimeout;

   internal MonitorCE(TimeSpan lockTimeout, TimeSpan waitTimeout)
   {
      LockTimeout = lockTimeout;
      WaitTimeout = waitTimeout;
      _readLock = new ReadLock(this);
      _updateLock = new UpdateLock(this);
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

   IDisposable TakeLock(LockType lockType, TimeSpan lockTimeout) => TryTakeLock(lockType, lockTimeout) ?? throw RegisterTimeoutException();

   IDisposable TakeLockWhen(Func<bool> condition, LockType lockType, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) => TryTakeLockWhen(condition, lockType, throwOnFailedLock: true, waitTimeout: waitTimeout, lockTimeout: lockTimeout) ?? throw new AwaitingConditionTimeoutException();

   IDisposable? TryTakeLock(LockType lockType, TimeSpan timeout) => _monitor.TryTakeLock(timeout) ? LockFor(lockType) : null;

   IDisposable? TryTakeLockWhen(Func<bool> condition, LockType lockType, bool throwOnFailedLock, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      var actualWaitTimeout = waitTimeout ?? WaitTimeout;
      var actualLockTimeout = lockTimeout ?? LockTimeout;

      var startTime = DateTime.UtcNow;

      IDisposable takenLock;
      if(throwOnFailedLock)
      {
         takenLock = TakeLock(lockType, actualLockTimeout);
      } else
      {
         var attemptedLock = TryTakeLock(lockType, actualLockTimeout);
         if(attemptedLock == null) return null;
         takenLock = attemptedLock;
      }

      while(!condition())
      {
         var elapsedTime = DateTime.UtcNow - startTime;
         var timeRemaining = actualWaitTimeout - elapsedTime;
         if(timeRemaining <= TimeSpan.Zero)
         {
            takenLock.Dispose();
            return null;
         }

         _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(timeRemaining);
      }

      return LockFor(lockType);
   }

   void ReleaseLock()
   {
      UpdateAnyRegisteredTimeoutExceptions();
      _monitor.ReleaseLock();
   }

   IReadOnlyList<EnterLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<EnterLockTimeoutException>();

   Exception RegisterTimeoutException()
   {
      lock(_timeoutLock)
      {
         var exception = new EnterLockTimeoutException(LockTimeout, _stackTraceFetchTimeout);
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

            Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<EnterLockTimeoutException>());
         }
      }
   }

   sealed class UpdateLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal UpdateLock(MonitorCE monitor) => _monitor = monitor;

      public void Dispose()
      {
         _monitor._monitor.NotifyWaitingThreadsAboutUpdates(); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.ReleaseLock();
      }
   }

   sealed class ReadLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal ReadLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.ReleaseLock();
   }

   enum LockType
   {
      Read = 0,
      Update = 1
   }
}
