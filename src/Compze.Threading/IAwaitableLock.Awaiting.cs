namespace Compze.Threading;

public partial interface IAwaitableLock
{
   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires. Returns false if the wait times out, else true.</summary>
   bool TryAwait(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, waitTimeout, lockTimeout);
      return readLock != null;
   }
}
