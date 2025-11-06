using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   public IDisposable TakeReadLock() => TakeReadLock(Timeout);

   public IDisposable TakeUpdateLock() => TakeUpdateLock(Timeout);

   bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? updateLock)
   {
      updateLock = TryTakeUpdateLockWhen(timeout, condition);
      return updateLock != null;
   }

   bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? readLock)
   {
      readLock = TryTakeReadLockWhen(timeout, condition);
      return readLock != null;
   }   
}
