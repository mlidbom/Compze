using System;
using System.Threading.Tasks;
using Composable.Functional;

namespace Composable.SystemCE;

static class VoidCEExtensions
{
   internal static Func<TParam, Unit> AsVoidFunc<TParam>(this Action<TParam> @this) =>
      param =>
      {
         @this(param);
         return Unit.Instance;
      };

   internal static Func<TParam, Task<Unit>> AsVoidFunc<TParam>(this Func<TParam, Task> @this) =>
      param =>
      {
         @this(param);
         return Unit.InstanceTask;
      };

   internal static Func<Unit> AsVoidFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Instance;
      };
}
