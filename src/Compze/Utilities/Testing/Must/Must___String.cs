using Compze.Utilities.SystemCE;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___String
{
   public static IMust<string> Contain(this IMust<string> must, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      must.SatisfyInternal(it => it.ContainsOrdinal(expected), usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<string> NotContain(this IMust<string> must, string unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      must.SatisfyInternal(it => !it.ContainsOrdinal(unexpected), usedArguments: [new(nameof(unexpected), unexpectedExpression, unexpected)]);

   public static IMust<string>? StartWith(this IMust<string> must, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      must.SatisfyInternal(it => it.StartsWithOrdinal(expected), usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IMust<string>? EndWith(this IMust<string> must, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      must.SatisfyInternal(it => it.EndsWithOrdinal(expected),usedArguments: [new(nameof(expected), expectedExpression, expected)]);
}
