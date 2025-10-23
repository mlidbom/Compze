using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Threading;

static class ActionExtensions
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

static class Throw<TException> where TException : Exception, new()
{
   internal static unit If(bool condition) => unit.From(() =>
   {
      if(condition) throw new TException();
   });
}
