using System;
using Composable.Functional;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   public delegate T OutParamFunc<T>(out T outParam);

   public TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(EnterLock()) return func();
   }

   public Unit Update(Action action)
   {
      using(EnterUpdateLock()) action();
      return Unit.Instance;
   }

   public T Update<T>(Func<T> func)
   {
      using(EnterUpdateLock()) return func();
   }

   public T Update<T>(OutParamFunc<T> func, out T outParam)
   {
      using(EnterUpdateLock()) return func(out outParam);
   }
}