namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires. Returns false if the wait times out, else true.</summary>
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   bool TryAwait(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, cancellationToken, waitTimeout, lockTimeout);
      return readLock != null;
   }
#pragma warning restore CA1068
}
