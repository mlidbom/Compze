using Compze.Utilities.Functional;
using System;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   unit Read(Action action, TimeSpan? timeout = null) => Read(action.AsFunc(), timeout);

   TReturn Read<TReturn>(Func<TReturn> func, TimeSpan? timeout = null)
   {
      using(TakeReadLock(timeout ?? Timeout)) return func();
   }

   unit Update(Action action) => Update(action.AsFunc());

   T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }


   unit ReadWhen(Action action, Func<bool> condition) => ReadWhen(action.AsFunc(), condition);
   TReturn ReadWhen<TReturn>(Func<TReturn> func, Func<bool> condition)
   {
      using(TakeReadLockWhen(condition)) return func();
   }

   unit UpdateWhen(Action action, Func<bool> condition) => ReadWhen(action.AsFunc(), condition);
   TReturn UpdateWhen<TReturn>(Func<TReturn> func, Func<bool> condition)
   {
      using(TakeUpdateLockWhen(condition)) return func();
   }
}
