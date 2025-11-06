using System;
using System.Diagnostics.CodeAnalysis;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

[SuppressMessage("Design",
                 "CA1001:Types that own disposable fields should be disposable",
                 Justification = "The lock fields are reusable tokens created once in the constructor for zero-allocation operations. They are not traditional disposable resources that need cleanup.")]
public partial class MonitorCE
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
