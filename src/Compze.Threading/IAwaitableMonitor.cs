namespace Compze.Threading;

public partial interface IAwaitableMonitor : IAwaitableLock
{
   public static IAwaitableMonitor WithDefaultTimeout() => new MonitorCE();
   public static IAwaitableMonitor New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new MonitorCE(lockTimeout, waitTimeout);

   internal static IMonitor NewIMonitor(LockTimeout? timeout = null) => new MonitorCE(timeout);
}
