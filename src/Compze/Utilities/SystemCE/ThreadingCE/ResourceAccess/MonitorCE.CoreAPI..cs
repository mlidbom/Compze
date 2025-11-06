using System;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
   IDisposable TakeLock(LockType lockType) => TakeLock(Timeout, lockType);
   IDisposable TakeLock(TimeSpan timeout, LockType lockType) => TryTakeLock(timeout, lockType) ?? throw RegisterTimeoutException();


   IDisposable TakeLockWhen(TimeSpan timeout, Func<bool> condition, LockType lockType) => TryTakeLockWhen(timeout, condition, lockType) ?? throw new AwaitingConditionTimeoutException();
   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Update);
   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Read);

   IDisposable? TryTakeLock(TimeSpan timeout, LockType lockType)
   {
      if(Monitor.TryEnter(_lockObject)) return LockFor(lockType); //This will never block and calling it first improves performance quite a bit.

      var lockTaken = false;
      try
      {
         Monitor.TryEnter(_lockObject, timeout, ref lockTaken);
         return lockTaken ? LockFor(lockType) : null;
      }
      catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
      {
         if(lockTaken) Monitor.Exit(_lockObject);
         ;
         throw;
      }
   }

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

         ReleaseLockAndReacquireItOnPulseOrTimeout(timeRemaining);
      }

      return LockFor(lockType);
   }
}
