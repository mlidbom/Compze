using System.Runtime.CompilerServices;

namespace Compze.Must;
// ReSharper disable InconsistentNaming

/// <summary>DateTime equality assertion with a tolerance.</summary>
public static class Must_Be_DateTime
{
   /// <summary>Asserts that the <see cref="DateTime"/> is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
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
