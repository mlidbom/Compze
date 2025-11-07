using Compze.Utilities.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitorCE
{
   unit Await(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      using var readLock = TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return unit.Value;
   }

   bool TryAwait(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);
      return readLock != null;
   }
}
