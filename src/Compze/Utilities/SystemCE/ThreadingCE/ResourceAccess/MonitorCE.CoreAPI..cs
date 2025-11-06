using System;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
   internal IDisposable TakeReadLock(TimeSpan timeout) => TakeLock(timeout, LockType.Read);
   public IDisposable TakeUpdateLock(TimeSpan timeout) => TakeLock(timeout, LockType.Update);

   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Update);
   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Read);


   IDisposable TakeLock(LockType lockType) => TakeLock(Timeout, lockType);
   IDisposable TakeLock(TimeSpan timeout, LockType lockType) => TryTakeLock(timeout, lockType) ?? throw RegisterTimeoutException();


   IDisposable TakeLockWhen(TimeSpan timeout, Func<bool> condition, LockType lockType) => TryTakeLockWhen(timeout, condition, lockType) ?? throw new AwaitingConditionTimeoutException();

   IDisposable? TryTakeLock(TimeSpan timeout, LockType lockType) => _coreLock.TryTakeLock(timeout) ? LockFor(lockType) : null;

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

         _coreLock.ReleaseLockAndReacquireItOnPulseOrTimeout(timeRemaining);
      }

      return LockFor(lockType);
   }
}
