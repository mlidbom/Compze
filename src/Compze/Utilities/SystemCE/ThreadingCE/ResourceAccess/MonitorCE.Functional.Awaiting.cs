using System;
using System.Threading;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   enum LockType
   {
      Read = 0,
      Update = 1
   }

   internal IDisposable EnterUpdateLockWhen(Func<bool> condition) =>
      EnterUpdateLockWhen(Timeout, condition);

   public IDisposable EnterUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Update);

   internal unit Await(Func<bool> condition) => Await(Timeout, condition);

   internal unit Await(TimeSpan conditionTimeout, Func<bool> condition) => Throw<AwaitingConditionTimeoutException>.If(!TryAwait(conditionTimeout, condition));

   internal bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
   {
      using var readLock = TryTakeReadLockWhen(conditionTimeout, condition);
      return readLock != null;
   }

   IDisposable TakeLockWhen(TimeSpan timeout, Func<bool> condition, LockType lockType) => TryTakeLockWhen(timeout, condition, lockType) ?? throw new AwaitingConditionTimeoutException();

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

         Wait(timeRemaining);
      }

      return LockFor(lockType);
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

   void Wait(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);
}
