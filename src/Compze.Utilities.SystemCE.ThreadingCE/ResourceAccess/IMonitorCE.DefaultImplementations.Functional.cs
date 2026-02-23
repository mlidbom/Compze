using Compze.Utilities.Functional;
using System;
using Compze.Utilities.Functional.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitorCE
{
   unit Read(Action action, TimeSpan? timeout = null) => Read(action.AsFunc(), timeout);

   TReturn Read<TReturn>(Func<TReturn> func, TimeSpan? timeout = null)
   {
      using(TakeReadLock(timeout)) return func();
   }

   unit Update(Action action, TimeSpan? timeout = null) => Update(action.AsFunc(), timeout);

   T Update<T>(Func<T> func, TimeSpan? timeout = null)
   {
      using(TakeUpdateLock(timeout)) return func();
   }

   unit ReadWhen(Action action, Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) =>
      ReadWhen(action.AsFunc(), condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   TReturn ReadWhen<TReturn>(Func<TReturn> func, Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   unit UpdateWhen(Action action, Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null) =>
      UpdateWhen(action.AsFunc(), condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout);

   TReturn UpdateWhen<TReturn>(Func<TReturn> func, Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }
}
