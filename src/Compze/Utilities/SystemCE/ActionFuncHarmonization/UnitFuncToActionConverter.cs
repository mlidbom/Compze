using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

static class UnitFuncToActionConverter
{
   internal static Action AsAction(this Func<unit> @this) =>
      () => @this();

   internal static Action<TParam> AsAction<TParam>(this Func<TParam, unit> @this) =>
      param => @this(param);

   internal static Action<TParam, TParam2> AsAction<TParam, TParam2>(this Func<TParam, TParam2, unit> @this) =>
      (param, param2) => @this(param, param2);
}
