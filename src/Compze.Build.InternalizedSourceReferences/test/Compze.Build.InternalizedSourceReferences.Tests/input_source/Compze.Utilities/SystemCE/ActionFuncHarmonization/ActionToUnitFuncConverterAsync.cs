using System;
using System.Threading.Tasks;
using Compze.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

public static class ActionToUnitFuncConverterAsync
{
   public static Func<Task<unit>> AsFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this().caf();
         return unit.Value;
      };

   public static Func<TParam,Task<unit>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param).caf();
         return unit.Value;
      };

   public static Func<TParam, TParam2,Task<unit>> AsFunc<TParam, TParam2>(this Func<TParam, TParam2, Task> @this) =>
      async (param, param2)  =>
      {
         await @this(param, param2).caf();
         return unit.Value;
      };
}
