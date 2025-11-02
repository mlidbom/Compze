using System;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must___IComparableAssertions
{
   public static Must<TValue> BeGreaterThan<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) > 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static Must<TValue> BeGreaterThanOrEqualTo<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) >= 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static Must<TValue> BeLessThan<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) < 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static Must<TValue> BeLessThanOrEqualTo<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(expected) <= 0,
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static Must<TValue> BePositive<TValue>(this Must<TValue> must)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(default(TValue)!) > 0);

   public static Must<TValue> BeNegative<TValue>(this Must<TValue> must)
      where TValue : IComparable<TValue> =>
      must.Satisfy(it => it.CompareTo(default(TValue)!) < 0);
}
