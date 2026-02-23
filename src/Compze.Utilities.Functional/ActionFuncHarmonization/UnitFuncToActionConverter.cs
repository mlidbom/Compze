using System;

namespace Compze.Utilities.Functional.ActionFuncHarmonization;

public static class UnitFuncToActionConverter
{
   public static Action AsAction(this Func<unit> @this) =>
      () => @this();

   public static Action<TParam> AsAction<TParam>(this Func<TParam, unit> @this) =>
      param => @this(param);

   public static Action<TParam, TParam2> AsAction<TParam, TParam2>(this Func<TParam, TParam2, unit> @this) =>
      (param, param2) => @this(param, param2);
}
