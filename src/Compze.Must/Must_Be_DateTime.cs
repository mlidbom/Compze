using System.Runtime.CompilerServices;

namespace Compze.Must;
// ReSharper disable InconsistentNaming

public static class Must_Be_DateTime
{
   public static IAssertionContext<DateTime> Be(this IAssertionContext<DateTime> context, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!)
   {
      return context.SatisfyInternal(
         it => (it - expected).Duration() <= tolerance,
         expressionValues:
         [
            new(expectedExpression, expected),
            new(toleranceExpression, tolerance)
         ]);
   }
}
