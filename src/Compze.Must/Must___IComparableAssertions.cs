using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions over <see cref="IComparable{T}"/> values.</summary>
public static class Must___IComparableAssertions
{
   /// <summary>Asserts that the value is greater than <paramref name="expected"/>.</summary>
   public static IAssertionContext<TValue> BeGreaterThan<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(expected) > 0,
                   expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the value is greater than or equal to <paramref name="expected"/>.</summary>
   public static IAssertionContext<TValue> BeGreaterThanOrEqualTo<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(expected) >= 0,
                   expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the value is less than <paramref name="expected"/>.</summary>
   public static IAssertionContext<TValue> BeLessThan<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(expected) < 0,
                   expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the value is less than or equal to <paramref name="expected"/>.</summary>
   public static IAssertionContext<TValue> BeLessThanOrEqualTo<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(expected) <= 0,
                   expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the value compares greater than its type's default.</summary>
   public static IAssertionContext<TValue> BePositive<TValue>(this IAssertionContext<TValue> context)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(default(TValue)!) > 0);

   /// <summary>Asserts that the value compares less than its type's default.</summary>
   public static IAssertionContext<TValue> BeNegative<TValue>(this IAssertionContext<TValue> context)
      where TValue : IComparable<TValue> =>
      context.RunAssertion(it => it.CompareTo(default(TValue)!) < 0);
}
