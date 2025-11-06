using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryEnterWhen(timeout, condition) ? _updateLock : null;
   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition) => TryEnterWhen(timeout, condition) ? _readLock : null;
   public IDisposable? TryTakeUpdateLock(TimeSpan timeout) => TryEnter(timeout) ? _updateLock : null;
   public IDisposable? TryTakeReadLock(TimeSpan timeout) => TryEnter(timeout) ? _readLock : null;
}
