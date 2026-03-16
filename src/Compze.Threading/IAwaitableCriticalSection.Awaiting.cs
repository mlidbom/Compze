namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires. Returns false if the wait times out, else true.</summary>
   bool TryAwait(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, cancellationToken, waitTimeout, lockTimeout);
      return readLock != null;
   }
}
