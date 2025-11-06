using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitorCE
{
   bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? updateLock)
   {
      updateLock = TryTakeUpdateLockWhen(condition, timeout);
      return updateLock != null;
   }

   bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? readLock)
   {
      readLock = TryTakeReadLockWhen(condition, timeout);
      return readLock != null;
   }
}
