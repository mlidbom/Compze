using System.Runtime.CompilerServices;

namespace Compze.Must;
// ReSharper disable InconsistentNaming
/// <summary>Reference-identity assertions.</summary>
public static class ReferenceEqualityAssertions
{
   /// <summary>Asserts that the value is the same instance as <paramref name="expected"/>.</summary>
   public static IAssertionContext<TValue> ReferenceEqual<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : class =>
      context.RunAssertion(it => ReferenceEquals(it, expected), expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the value is not the same instance as <paramref name="unexpected"/>.</summary>
   public static IAssertionContext<TValue> NotReferenceEqual<TValue>(this IAssertionContext<TValue> context, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
      where TValue : class =>
      context.RunAssertion(it => !ReferenceEquals(it, unexpected), expressionValues: [new(unexpectedExpression, unexpected)]);
}
