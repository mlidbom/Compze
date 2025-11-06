using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

interface IMonitorCE
{
   public static IMonitorCE WithDefaultTimeout() => MonitorCE.WithDefaultTimeout();
   public static IMonitorCE WithTimeout(TimeSpan timeout) => MonitorCE.WithTimeout(timeout);

   //the core, the functionality implemented by the actual MonitorCE implementation, upon which everything else is convenience extensions.
   TimeSpan Timeout { get; }

   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);

   bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? updateLock)
   {
      updateLock = TryTakeUpdateLockWhen(timeout, condition);
      return updateLock != null;
   }

   bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? readLock)
   {
      readLock = TryTakeReadLockWhen(timeout, condition);
      return readLock != null;
   }
}
