using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class LockCE
{
   enum LockType
   {
      Read = 0,
      Update = 1
   }

   public IDisposable TakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Update);
   public IDisposable TakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TakeLockWhen(timeout, condition, LockType.Read);

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
