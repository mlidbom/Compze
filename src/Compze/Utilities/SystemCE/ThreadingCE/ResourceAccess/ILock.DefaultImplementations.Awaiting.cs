using Compze.Utilities.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   IDisposable TakeReadLockWhen(Func<bool> condition) => TakeReadLockWhen(Timeout, condition);
   IDisposable TakeUpdateLockWhen(Func<bool> condition) => TakeUpdateLockWhen(Timeout, condition);

   unit Await(Func<bool> condition) => Await(condition, Timeout);

   unit Await(Func<bool> condition, TimeSpan conditionTimeout)
   {
      using(TakeReadLockWhen(conditionTimeout, condition));
      return unit.Value;
   }

   bool TryAwait(Func<bool> condition, TimeSpan conditionTimeout)
   {
      using var readLock = TryTakeReadLockWhen(conditionTimeout, condition);
      return readLock != null;
   }
}
