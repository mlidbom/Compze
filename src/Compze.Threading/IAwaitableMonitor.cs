namespace Compze.Threading;

public partial interface IAwaitableMonitor : ILockInfo
{
   public static IAwaitableMonitor WithDefaultTimeout() => new LockCE();
   public static IAwaitableMonitor New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new LockCE(lockTimeout, waitTimeout);

   internal static IMonitor NewIMonitor(LockTimeout? timeout = null) => new LockCE(timeout);

   IDisposable TakeReadLock(LockTimeout? timeout = null);
   IDisposable TakeUpdateLock(LockTimeout? timeout = null);

   IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   WaitTimeout WaitTimeout { get; }
}
