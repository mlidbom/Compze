using Compze.Utilities.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   unit Await(Func<bool> condition) => Await(condition, Timeout);

   unit Await(Func<bool> condition, TimeSpan conditionTimeout)
   {
      using var readLock = TakeReadLockWhen(condition, conditionTimeout);
      return unit.Value;
   }

   bool TryAwait(Func<bool> condition, TimeSpan conditionTimeout)
   {
      using var readLock = TryTakeReadLockWhen(condition, conditionTimeout);
      return readLock != null;
   }
}
