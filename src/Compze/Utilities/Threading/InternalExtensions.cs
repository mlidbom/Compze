using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Threading;

static class ActionExtensions
{
   internal static Func<unit> AsUnitFunc(this Action @this) =>
      () =>
      {
         @this();
         return unit.Value;
      };
}

static class Throw<TException> where TException : Exception, new()
{
   internal static void If(bool condition)
   {
      if(condition) throw new TException();
   }
}
