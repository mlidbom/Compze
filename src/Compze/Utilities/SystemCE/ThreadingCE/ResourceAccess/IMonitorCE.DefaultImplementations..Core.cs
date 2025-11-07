using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitorCE
{
   bool TryTakeUpdateLockWhen(Func<bool> condition, [NotNullWhen(true)] out IDisposable? updateLock, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      updateLock = TryTakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return updateLock != null;
   }

   bool TryTakeReadLockWhen(Func<bool> condition, [NotNullWhen(true)] out IDisposable? readLock, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      readLock = TryTakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return readLock != null;
   }
}
