using System;
using System.Threading.Tasks;

namespace Compze.Utilities.Functional.ActionFuncHarmonization;

public static class ActionToUnitFuncConverterAsync
{
   public static Func<Task<unit>> AsFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this().ConfigureAwait(false);
         return unit.Value;
      };

   public static Func<TParam,Task<unit>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param).ConfigureAwait(false);
         return unit.Value;
      };

   public static Func<TParam, TParam2,Task<unit>> AsFunc<TParam, TParam2>(this Func<TParam, TParam2, Task> @this) =>
      async (param, param2)  =>
      {
         await @this(param, param2).ConfigureAwait(false);
         return unit.Value;
      };
}
