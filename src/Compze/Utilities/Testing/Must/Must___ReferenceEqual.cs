using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must___ReferenceEqual
{
   public static IAssertionContext<TValue> ReferenceEqual<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : class =>
      context.SatisfyInternal(it => ReferenceEquals(it, expected), expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<TValue> NotReferenceEqual<TValue>(this IAssertionContext<TValue> context, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
      where TValue : class =>
      context.SatisfyInternal(it => !ReferenceEquals(it, unexpected), expressionValues: [new(unexpectedExpression, unexpected)]);
}
