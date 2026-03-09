namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
   TReturn Read<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   TReturn ReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   unit Update(Action action, LockTimeout? timeout = null) => Update(action.ToFunc(), timeout);

   T Update<T>(Func<T> func, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   unit UpdateWhen<TReturn>(Func<bool> condition, Action action, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      UpdateWhen(condition, action.ToFunc(), waitTimeout, lockTimeout);

   TReturn UpdateWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }
}
