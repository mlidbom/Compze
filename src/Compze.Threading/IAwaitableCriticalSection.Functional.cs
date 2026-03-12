using Compze.SystemCE;

namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
   ///<summary>Acquires a read lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Read<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, acquires a read lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn ReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Acquires an update lock, executes <paramref name="action"/>, then releases the lock.</summary>
   Unit Update(Action action, LockTimeout? timeout = null) => Update(action.ToFunc(), timeout);

   ///<summary>Acquires an update lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   T Update<T>(Func<T> func, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, acquires an update lock, executes <paramref name="action"/>, then releases the lock.</summary>
   Unit UpdateWhen<TReturn>(Func<bool> condition, Action action, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      UpdateWhen(condition, action.ToFunc(), waitTimeout, lockTimeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true, acquires an update lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn UpdateWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, acquires an update lock, and executes <paramref name="action"/>. Returns false if the wait times out, else true.</summary>
   bool TryUpdateWhen(Func<bool> condition, Action action, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var updateLock = TryTakeUpdateLockWhen(condition, waitTimeout, lockTimeout);
      if(updateLock == null) return false;
      action();
      return true;
   }
}
