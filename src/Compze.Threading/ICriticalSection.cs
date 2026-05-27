namespace Compze.Threading;

///<summary>A mutual-exclusion critical section. Dispose the returned <see cref="ILock"/> to release the lock.</summary>
public partial interface ICriticalSection : ICriticalSectionInfo
{
   ///<summary>Acquires the lock, blocking until it is available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> if <paramref name="timeout"/> expires. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);
#pragma warning restore CA1068
}
