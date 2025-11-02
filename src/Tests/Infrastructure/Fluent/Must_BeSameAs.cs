using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must_BeSameAs
{
   public static Must<TValue> BeSameAs<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : class =>
      must.Satisfy(it => ReferenceEquals(it, expected),
                   usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static Must<TValue> NotBeSameAs<TValue>(this Must<TValue> must, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
      where TValue : class =>
      must.Satisfy(it => !ReferenceEquals(it, unexpected),
                   usedArguments: [new(nameof(unexpected), unexpectedExpression, unexpected)]);
}
