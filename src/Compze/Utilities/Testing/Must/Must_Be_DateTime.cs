using System;
using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must_Be_DateTime
{
   public static IAssertionContext<DateTime> Be(this IAssertionContext<DateTime> assertionContext, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!)
   {
      return assertionContext.SatisfyInternal(
         it => (it - expected).Duration() <= tolerance,
         usedArguments:
         [
            new(nameof(expected), expectedExpression, expected),
            new(nameof(tolerance), toleranceExpression, tolerance)
         ]);
   }
}
