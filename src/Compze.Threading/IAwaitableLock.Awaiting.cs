namespace Compze.Threading;

public partial interface IAwaitableLock
{
   bool TryAwait(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, waitTimeout, lockTimeout);
      return readLock != null;
   }
}
