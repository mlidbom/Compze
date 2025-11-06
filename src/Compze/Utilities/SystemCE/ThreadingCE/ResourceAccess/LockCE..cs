using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible. They are not normal disposables
///<summary>The monitor class exposes a rather obscure, brittle and easily misused API in my opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
class LockCE : ILock
#pragma warning restore CA1001
{
   public TimeSpan Timeout { get; }

   public IDisposable TakeReadLock(TimeSpan? timeout = null) => TakeLock(LockType.Read, timeout ?? Timeout);
   public IDisposable TakeUpdateLock(TimeSpan? timeout = null) => TakeLock(LockType.Update, timeout ?? Timeout);

   public IDisposable TakeReadLockWhen(Func<bool> condition, TimeSpan? timeout = null) => TakeLockWhen(condition, LockType.Read, timeout ?? Timeout);
   public IDisposable TakeUpdateLockWhen(Func<bool> condition, TimeSpan? timeout = null) => TakeLockWhen(condition, LockType.Update, timeout ?? Timeout);

   public IDisposable? TryTakeReadLockWhen(Func<bool> condition, TimeSpan? timeout = null) => TryTakeLockWhen(condition, LockType.Read, timeout ?? Timeout);
   public IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, TimeSpan? timeout = null) => TryTakeLockWhen(condition, LockType.Update, timeout ?? Timeout);

   public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;


   readonly MonitorCE _monitor = new();
   readonly Lock _timeoutLock = new();

   readonly IDisposable _readLock;
   readonly IDisposable _updateLock;

   static readonly TimeSpan DefaultTimeToWaitForStackTrace = 1.Seconds();

   TimeSpan _stackTraceFetchTimeout;

   internal LockCE(TimeSpan timeout)
   {
      Timeout = timeout;
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

   IDisposable TakeLock(LockType lockType, TimeSpan timeout) => TryTakeLock(lockType, timeout) ?? throw RegisterTimeoutException();

   IDisposable TakeLockWhen(Func<bool> condition, LockType lockType, TimeSpan timeout) => TryTakeLockWhen(condition, lockType, timeout) ?? throw new AwaitingConditionTimeoutException();

   IDisposable? TryTakeLock(LockType lockType, TimeSpan timeout) => _monitor.TryTakeLock(timeout) ? LockFor(lockType) : null;

   IDisposable? TryTakeLockWhen(Func<bool> condition, LockType lockType, TimeSpan timeout)
   {
      var startTime = DateTime.UtcNow;
      if(TryTakeLock(lockType, timeout) is not {} takenLock)
         return null;

      while(!condition())
      {
         var elapsedTime = DateTime.UtcNow - startTime;
         var timeRemaining = timeout - elapsedTime;
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
         var exception = new EnterLockTimeoutException(Timeout, _stackTraceFetchTimeout);
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
      readonly LockCE _monitor;
      internal UpdateLock(LockCE monitor) => _monitor = monitor;

      public void Dispose()
      {
         _monitor._monitor.NotifyWaitingThreadsAboutUpdates(); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.ReleaseLock();
      }
   }

   sealed class ReadLock : IDisposable
   {
      readonly LockCE _monitor;
      internal ReadLock(LockCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.ReleaseLock();
   }

   enum LockType
   {
      Read = 0,
      Update = 1
   }
}
