using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

public static class ActionToUnitFuncConverter
{
   public static Func<unit> AsFunc(this Action @this) =>
      () =>
      {
         @this();
         return unit.Value;
      };

   public static Func<TParam, unit> AsFunc<TParam>(this Action<TParam> @this) =>
      param => unit.From(() => @this(param));

   public static Func<TParam, TParam2, unit> AsFunc<TParam, TParam2>(this Action<TParam, TParam2> @this) =>
      (param, param2) => unit.From(() => @this(param, param2));
}
