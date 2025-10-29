using System;
using System.Threading.Tasks;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

static class ActionToUnitFuncConverterAsync
{
   internal static Func<Task<unit>> AsUnitFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this();
         return unit.Value;
      };

   internal static Func<TParam,Task<unit>> AsUnitFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param);
         return unit.Value;
      };

   internal static Func<TParam, TParam2,Task<unit>> AsUnitFunc<TParam, TParam2>(this Func<TParam, TParam2, Task> @this) =>
      async (param, param2)  =>
      {
         await @this(param, param2);
         return unit.Value;
      };
}
