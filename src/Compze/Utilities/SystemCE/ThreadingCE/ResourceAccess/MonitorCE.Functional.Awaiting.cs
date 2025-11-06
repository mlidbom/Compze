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


   IDisposable LockFor(LockType lockType)
   {
      return lockType switch
      {
         LockType.Read   => _readLock,
         LockType.Update => _updateLock,
         _               => throw new ArgumentOutOfRangeException(nameof(lockType), lockType, message: null)
      };
   }

   void ReleaseLockAndReacquireItOnPulseOrTimeout(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);
}
