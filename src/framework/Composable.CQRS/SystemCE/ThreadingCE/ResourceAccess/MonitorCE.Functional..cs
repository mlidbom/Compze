using System;
using Composable.Functional;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   public delegate T OutParamFunc<T>(out T outParam);

   public TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(TakeReadLock()) return func();
   }

   public Unit Update(Action action) => Update(action.AsUnitFunc());

   public T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }

   public T Update<T>(OutParamFunc<T> func, out T outParam)
   {
      using(TakeUpdateLock()) return func(out outParam);
   }
}