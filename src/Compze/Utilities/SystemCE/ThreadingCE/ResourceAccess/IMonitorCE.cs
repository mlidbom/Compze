using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

interface IMonitorCE
{
   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeUpdateLock(TimeSpan timeout);
   IDisposable? TryTakeReadLock(TimeSpan timeout);
}
