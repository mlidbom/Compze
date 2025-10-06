using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE;

static class VoidCEExtensions
{
   internal static Func<TParam, Unit> AsUnitFunc<TParam>(this Action<TParam> @this) =>
      param =>
      {
         @this(param);
         return Unit.Instance;
      };

   internal static Func<Unit> AsUnitFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Instance;
      };
}
