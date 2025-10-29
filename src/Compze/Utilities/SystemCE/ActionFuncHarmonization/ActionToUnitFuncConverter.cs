using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

static class ActionToUnitFuncConverter
{
   internal static Func<unit> AsUnitFunc(this Action @this) =>
      () =>
      {
         @this();
         return unit.Value;
      };

   internal static Func<TParam, unit> AsUnitFunc<TParam>(this Action<TParam> @this) =>
      param => unit.From(() => @this(param));

   internal static Func<TParam, TParam2, unit> AsUnitFunc<TParam, TParam2>(this Action<TParam, TParam2> @this) =>
      (param, param2) => unit.From(() => @this(param, param2));
}
