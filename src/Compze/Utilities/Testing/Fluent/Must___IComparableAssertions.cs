using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Fluent;

public static class Must___IComparableAssertions
{
   public static IMust<TValue> BeGreaterThan<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) > 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<TValue> BeGreaterThanOrEqualTo<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) >= 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<TValue> BeLessThan<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) < 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<TValue> BeLessThanOrEqualTo<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) <= 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<TValue> BePositive<TValue>(this IMust<TValue> must)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(default(TValue)!) > 0);

   public static IMust<TValue> BeNegative<TValue>(this IMust<TValue> must)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(default(TValue)!) < 0);
}
