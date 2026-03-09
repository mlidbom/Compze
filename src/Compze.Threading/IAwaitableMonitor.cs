namespace Compze.Threading;

public partial interface IAwaitableMonitor : IAwaitableLock
{
   public static IAwaitableMonitor WithDefaultTimeout() => new LockCE();
   public static IAwaitableMonitor New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new LockCE(lockTimeout, waitTimeout);

   internal static IMonitor NewIMonitor(LockTimeout? timeout = null) => new LockCE(timeout);
}
