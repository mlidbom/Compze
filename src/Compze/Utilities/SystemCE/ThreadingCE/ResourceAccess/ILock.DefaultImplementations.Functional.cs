using Compze.Utilities.Functional;
using System;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
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

   unit ReadWhen(Action action, Func<bool> condition, TimeSpan? timeout = null) => ReadWhen(action.AsFunc(), condition, timeout);

   TReturn ReadWhen<TReturn>(Func<TReturn> func, Func<bool> condition, TimeSpan? timeout = null)
   {
      using(TakeReadLockWhen(condition, timeout)) return func();
   }

   unit UpdateWhen(Action action, Func<bool> condition, TimeSpan? timeout = null) => ReadWhen(action.AsFunc(), condition, timeout);

   TReturn UpdateWhen<TReturn>(Func<TReturn> func, Func<bool> condition, TimeSpan? timeout = null)
   {
      using(TakeUpdateLockWhen(condition)) return func();
   }
}
