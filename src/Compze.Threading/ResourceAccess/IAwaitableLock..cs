namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableLock
{
   public static IAwaitableLock WithDefaultTimeout() => new LockCE();
   public static IAwaitableLock New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new LockCE(lockTimeout, waitTimeout);

   internal static ILock NewIMonitor(LockTimeout? timeout = null) => new LockCE(timeout);

   IDisposable TakeReadLock(LockTimeout? timeout = null);
   IDisposable TakeUpdateLock(LockTimeout? timeout = null);

   IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   long ContentionCount { get; }
}
