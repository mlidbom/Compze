using Compze.Underscore;
using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableMonitor
{
   TReturn Read<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   unit Update(Action action, LockTimeout? timeout = null) => Update(action.AsFunc(), timeout);

   T Update<T>(Func<T> func, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   TReturn ReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout, lockTimeout)) return func();
   }

   TReturn UpdateWhen<TReturn>(Func<bool> condition, Func<TReturn> func, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout, lockTimeout)) return func();
   }
}
