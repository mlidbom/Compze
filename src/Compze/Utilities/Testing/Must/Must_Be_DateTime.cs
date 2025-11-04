using System;
using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.Fluent;
// ReSharper disable InconsistentNaming

public static class Must_Be_DateTime
{
   public static IMust<DateTime> Be(this IMust<DateTime> must, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!)
   {
      return must.SatisfyInternal(
         it => (it - expected).Duration() <= tolerance,
         usedArguments:
         [
            new(nameof(expected), expectedExpression, expected),
            new(nameof(tolerance), toleranceExpression, tolerance)
         ]);
   }
}
