namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableMonitor
{
   public static IAwaitableMonitor WithDefaultTimeout() => new MonitorCE();
   public static IAwaitableMonitor New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new MonitorCE(lockTimeout, waitTimeout);

   internal static IMonitor NewIMonitor(LockTimeout? timeout = null) => new MonitorCE(timeout);

   IDisposable TakeReadLock(LockTimeout? timeout = null);
   IDisposable TakeUpdateLock(LockTimeout? timeout = null);

   IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   long ContentionCount { get; }
}
