using System;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class DateTimeToleranceAssertions
{
   public static Must<DateTime> Be(this Must<DateTime> must, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!)
   {
      return must.Satisfy(
         it => (it - expected).Duration() <= tolerance,
         usedArguments:
         [
            new(nameof(expected), expectedExpression, expected),
            new(nameof(tolerance), toleranceExpression, tolerance)
         ]);
   }
}
