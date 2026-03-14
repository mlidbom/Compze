using Compze.SystemCE;

namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
   ///<summary>Acquires a read lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Read<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, then - within the same read lock held while executing <paramref name="condition"/> - executes <paramref name="func"/> and returns its result.</summary>
   TReturn ReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Executes <paramref name="action"/> within an update lock, notifying all waiting threads that there are updates.</summary>
   Unit Update(Action action, LockTimeout? timeout = null) => Update(action.ToFunc(), timeout);

   ///<summary>Executes <paramref name="func"/> within an update lock, notifying all waiting threads that there are updates.</summary>
   T Update<T>(Func<T> func, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, then upgrades its read lock to an update lock, and executes <paramref name="func"/> within that lock, notifying all waiting threads that there are updates.</summary>
   Unit UpdateWhen<TReturn>(Func<bool> condition, Action action, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      UpdateWhen(condition, action.ToFunc(), waitTimeout, lockTimeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true, then upgrades its read lock to an update lock, and executes <paramref name="func"/> within that lock, notifying all waiting threads that there are updates.</summary>
   TReturn UpdateWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires, <br/>
   /// if <paramref name="waitTimeout"/> did not expire, upgrades its read lock to an update lock and executes <paramref name="action"/> -notifying all waiting threads that there are updates - and returns true. <br/>
   /// If <paramref name="waitTimeout"/> expired, returns false without doing anything.</summary>
   bool TryUpdateWhen(Func<bool> condition, Action action, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var updateLock = TryTakeUpdateLockWhen(condition, waitTimeout, lockTimeout);
      if(updateLock == null) return false;
      action();
      return true;
   }
}