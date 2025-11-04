using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must___ReferenceEqual
{
   public static IMust<TValue> BeSameAs<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : class =>
      must.SatisfyInternal(it => ReferenceEquals(it, expected), usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<TValue> NotBeSameAs<TValue>(this IMust<TValue> must, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
      where TValue : class =>
      must.SatisfyInternal(it => !ReferenceEquals(it, unexpected), usedArguments: [new(nameof(unexpected), unexpectedExpression, unexpected)]);
}
