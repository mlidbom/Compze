using System;
using Compze.Utilities.Functional;

namespace Compze.Threading.Internal;

static class TimeSpanExtensions
{
   internal static TimeSpan Milliseconds(this int @this) => TimeSpan.FromMilliseconds(@this);
   internal static TimeSpan Seconds(this int @this) => TimeSpan.FromSeconds(@this);
   internal static TimeSpan Minutes(this int @this) => TimeSpan.FromMinutes(@this);
}

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
