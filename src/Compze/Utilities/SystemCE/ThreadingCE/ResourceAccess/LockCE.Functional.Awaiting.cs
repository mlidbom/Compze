using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class LockCE
{
   enum LockType
   {
      Read = 0,
      Update = 1
   }

   internal IDisposable TakeUpdateLockWhen(Func<bool> condition) => TakeUpdateLockWhen(Timeout, condition);

   public IDisposable TakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Update);
   public IDisposable TakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Read);

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
}
