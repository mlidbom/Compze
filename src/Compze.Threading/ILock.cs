namespace Compze.Threading;

public partial interface ILock : ILockInfo
{
   IDisposable TakeLock(LockTimeout? timeout = null);
}
