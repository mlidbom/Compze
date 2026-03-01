using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IMonitor
{
   public static IMonitor WithDefaultTimeout() => new MonitorCE(LockTimeout.Default, WaitTimeout.Default);
   public static IMonitor New(LockTimeout timeout) => new MonitorCE(timeout, WaitTimeout.Default);

   internal static IAwaitableMonitor CreateAwaitableWithDefaultTimeout() => new MonitorCE(LockTimeout.Default, WaitTimeout.Default);
   internal static IAwaitableMonitor CreateAwaitableWithTimeouts(LockTimeout lockTimeout, WaitTimeout waitTimeout) => new MonitorCE(lockTimeout, waitTimeout);

   LockTimeout LockTimeout { get; }

   IDisposable TakeLock(LockTimeout? timeout = null);

   long ContentionCount { get; }

   void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
}
