using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

[SuppressMessage("Design",
                 "CA1001:Types that own disposable fields should be disposable",
                 Justification = "The lock fields are reusable tokens created once in the constructor for zero-allocation operations. They are not traditional disposable resources that need cleanup.")]
partial class MonitorCE
{
   public TReturn Read<TReturn>(Func<TReturn> func)
   {
      using(TakeReadLock()) return func();
   }

   public unit Update(Action action) => Update(action.AsUnitFunc());

   public T Update<T>(Func<T> func)
   {
      using(TakeUpdateLock()) return func();
   }
}