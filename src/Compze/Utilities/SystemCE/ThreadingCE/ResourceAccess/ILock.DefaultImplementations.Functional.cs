using Compze.Utilities.Functional;
using System;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface ILock
{
   public unit Read(Action action) => Read(action.AsFunc());

   public TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(TakeReadLock()) return func();
   }

   public unit Update(Action action) => Update(action.AsFunc());

   public T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }
}
