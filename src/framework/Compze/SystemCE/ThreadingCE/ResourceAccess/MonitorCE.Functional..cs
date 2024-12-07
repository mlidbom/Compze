using System;
using Compze.Functional;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

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