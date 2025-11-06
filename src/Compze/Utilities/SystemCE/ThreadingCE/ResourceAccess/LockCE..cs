using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a rather obscure, brittle and easily misused API in my opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
class LockCE : ILock
{
   public TimeSpan Timeout { get; }

   public IDisposable TakeReadLock(TimeSpan timeout) => TakeLock(timeout, LockType.Read);
   public IDisposable TakeUpdateLock(TimeSpan timeout) => TakeLock(timeout, LockType.Update);

   public IDisposable TakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Read);
   public IDisposable TakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Update);

   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Read);
   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Update);

   public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;


   readonly MonitorCE _monitor = new();
   readonly Lock _timeoutLock = new();

#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible. They are not normal disposables
   readonly IDisposable _readLock;
   readonly IDisposable _updateLock;
#pragma warning restore CA1001

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

   IDisposable TakeLock(TimeSpan timeout, LockType lockType) => TryTakeLock(timeout, lockType) ?? throw RegisterTimeoutException();

   IDisposable TakeLockWhen(TimeSpan timeout, Func<bool> condition, LockType lockType) => TryTakeLockWhen(timeout, condition, lockType) ?? throw new AwaitingConditionTimeoutException();

   IDisposable? TryTakeLock(TimeSpan timeout, LockType lockType) => _monitor.TryTakeLock(timeout) ? LockFor(lockType) : null;

   IDisposable? TryTakeLockWhen(TimeSpan timeout, Func<bool> condition, LockType lockType)
   {
      var startTime = DateTime.UtcNow;
      if(TryTakeLock(timeout, lockType) is not {} takenLock)
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
