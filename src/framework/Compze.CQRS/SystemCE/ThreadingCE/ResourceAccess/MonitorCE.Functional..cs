using System;
using Compze.Functional;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   public delegate T OutParamFunc<T>(out T outParam);

   internal TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(TakeReadLock()) return func();
   }

   public Unit Update(Action action) => Update(action.AsUnitFunc());

   internal T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }

   public T Update<T>(OutParamFunc<T> func, out T outParam)
   {
      using(TakeUpdateLock()) return func(out outParam);
   }
}