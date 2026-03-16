namespace Compze.Threading;

///<summary>A mutual-exclusion critical section. Dispose the returned <see cref="ILock"/> to release the lock.</summary>
public partial interface ICriticalSection : ICriticalSectionInfo
{
   ///<summary>Acquires the lock, blocking until it is available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> if <paramref name="timeout"/> expires. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);
}
