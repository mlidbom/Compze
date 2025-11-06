using Compze.Utilities.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   IDisposable TakeUpdateLockWhen(Func<bool> condition) => TakeUpdateLockWhen(Timeout, condition);

   unit Await(Func<bool> condition) => Await(Timeout, condition);

   unit Await(TimeSpan conditionTimeout, Func<bool> condition) => Throw<AwaitingConditionTimeoutException>.If(!TryAwait(conditionTimeout, condition));

   bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
   {
      using var readLock = TryTakeReadLockWhen(conditionTimeout, condition);
      return readLock != null;
   }
}
