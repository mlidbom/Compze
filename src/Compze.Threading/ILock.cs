namespace Compze.Threading;

///<summary>A mutual-exclusion lock. Dispose the returned <see cref="IDisposable"/> to release the lock.</summary>
public partial interface ILock : ILockInfo
{
   ///<summary>Acquires the lock, blocking until it is available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> if <paramref name="timeout"/> expires. Uses <see cref="ILockInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IDisposable TakeLock(LockTimeout? timeout = null);
}
