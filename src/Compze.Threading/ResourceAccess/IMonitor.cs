using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IMonitor
{
   public static IMonitor New(LockTimeout? timeout = null) => new MonitorCE(timeout ?? LockTimeout.Default, WaitTimeout.Default);

   internal static IAwaitableMonitor CreateAwaitableWithDefaultTimeout() => new MonitorCE(LockTimeout.Default, WaitTimeout.Default);
   internal static IAwaitableMonitor CreateAwaitableWithTimeouts(LockTimeout lockTimeout, WaitTimeout waitTimeout) => new MonitorCE(lockTimeout, waitTimeout);

   IDisposable TakeLock(LockTimeout? timeout = null);
}
