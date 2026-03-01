using Compze.Underscore;
using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IAwaitableMonitor
{
   unit Read(Action action, LockTimeout? timeout = null) => Read(action.AsFunc(), timeout);

   TReturn Read<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   unit Update(Action action, LockTimeout? timeout = null) => Update(action.AsFunc(), timeout);

   T Update<T>(Func<T> func, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   unit ReadWhen(Action action, Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      ReadWhen(action.AsFunc(), condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   TReturn ReadWhen<TReturn>(Func<TReturn> func, Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   unit UpdateWhen(Action action, Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      UpdateWhen(action.AsFunc(), condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   TReturn UpdateWhen<TReturn>(Func<TReturn> func, Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }
}
