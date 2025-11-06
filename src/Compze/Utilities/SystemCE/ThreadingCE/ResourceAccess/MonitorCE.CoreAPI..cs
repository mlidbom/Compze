using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryTakeLockWhen(timeout, condition, LockType.Update);
   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition)  => TryTakeLockWhen(timeout, condition, LockType.Read);
}
