using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

interface IMonitorCE
{
   public static IMonitorCE WithDefaultTimeout() => MonitorCE.WithDefaultTimeout();
   public static IMonitorCE WithTimeout(TimeSpan timeout) => MonitorCE.WithTimeout(timeout);

   //the core, the functionality implemented by the actual MonitorCE implementation, upon which everything else is convenience extensions.
   TimeSpan Timeout { get; }

   public bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? takenLock);
   public bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? takenLock);



}
