using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableMonitor
{
   bool TryAwait(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, waitTimeout, lockTimeout);
      return readLock != null;
   }
}
