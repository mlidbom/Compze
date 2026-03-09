namespace Compze.Threading.ResourceAccess;

public partial interface ILock
{
   public static ILock New(LockTimeout? timeout = null) => IAwaitableLock.NewIMonitor(timeout);

   IDisposable TakeLock(LockTimeout? timeout = null);
   LockTimeout LockTimeout { get; }
   long ContentionCount { get; }
}
