using Compze.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IAwaitableMonitor
{
   unit Await(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return unit.Value;
   }

   bool TryAwait(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return readLock != null;
   }
}
