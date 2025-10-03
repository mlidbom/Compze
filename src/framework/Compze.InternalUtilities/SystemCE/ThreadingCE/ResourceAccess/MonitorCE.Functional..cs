using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

partial class MonitorCE
{
   public TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(TakeReadLock()) return func();
   }

   public Unit Update(Action action) => Update(action.AsUnitFunc());

   public T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }
}