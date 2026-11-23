using System.Runtime.CompilerServices;

namespace Compze.Must;
// ReSharper disable InconsistentNaming
/// <summary>
/// DateTime proximity assertion: equality within a tolerance.<br/>
/// For a check that no Compze.Must assertion makes, use <see cref="SatisfyAssertions.Satisfy{T}(IAssertionContext{T}, Func{T, bool}, Func{T, string}, string)"/>:<br/>
/// it takes any predicate, and its failure shows the predicate and the value.
/// </summary>
public static class DateTimeAssertions
{
   /// <summary>Asserts that the <see cref="DateTime"/> is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
   public static IAssertionContext<DateTime> BeCloseTo(this IAssertionContext<DateTime> context, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!)
   {
      return context.RunAssertion(
         it => (it - expected).Duration() <= tolerance,
         expressionValues:
         [
            new(expectedExpression, expected),
            new(toleranceExpression, tolerance)
         ]);
   }
}
