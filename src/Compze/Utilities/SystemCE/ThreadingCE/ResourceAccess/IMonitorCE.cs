using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

interface IMonitorCE
{
   //the core, the functionality implemented by the actual MonitorCE implementation, upon which everything else is convenience extensions.
   TimeSpan Timeout { get; }
   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeUpdateLock(TimeSpan timeout);
   IDisposable? TryTakeReadLock(TimeSpan timeout);
}
