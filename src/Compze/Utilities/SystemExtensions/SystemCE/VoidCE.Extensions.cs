using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE;

static class VoidCEExtensions
{
   internal static Func<TParam, unit> AsUnitFunc<TParam>(this Action<TParam> @this) =>
      param =>
      {
         @this(param);
         return unit.Value;
      };

   internal static Func<unit> AsUnitFunc(this Action @this) =>
      () =>
      {
         @this();
         return unit.Value;
      };
}
