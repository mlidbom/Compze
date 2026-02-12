using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___IComparableAssertions
{
   public static IAssertionContext<TValue> BeGreaterThan<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(expected) > 0,
                   expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<TValue> BeGreaterThanOrEqualTo<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(expected) >= 0,
                   expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<TValue> BeLessThan<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(expected) < 0,
                   expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<TValue> BeLessThanOrEqualTo<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(expected) <= 0,
                   expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<TValue> BePositive<TValue>(this IAssertionContext<TValue> context)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(default(TValue)!) > 0);

   public static IAssertionContext<TValue> BeNegative<TValue>(this IAssertionContext<TValue> context)
      where TValue : IComparable<TValue> =>
      context.SatisfyInternal(it => it.CompareTo(default(TValue)!) < 0);
}
