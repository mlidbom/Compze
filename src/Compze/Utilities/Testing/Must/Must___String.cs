using Compze.Utilities.SystemCE;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___String
{
   public static IAssertionContext<string> Contain(this IAssertionContext<string> assertionContext, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      assertionContext.SatisfyInternal(it => it.ContainsOrdinal(expected), usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IAssertionContext<string> NotContain(this IAssertionContext<string> assertionContext, string unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      assertionContext.SatisfyInternal(it => !it.ContainsOrdinal(unexpected), usedArguments: [new(nameof(unexpected), unexpectedExpression, unexpected)]);

   public static IAssertionContext<string>? StartWith(this IAssertionContext<string> assertionContext, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      assertionContext.SatisfyInternal(it => it.StartsWithOrdinal(expected), usedArguments: [new(nameof(expected), expectedExpression, expected)]);

   public static IAssertionContext<string>? EndWith(this IAssertionContext<string> assertionContext, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      assertionContext.SatisfyInternal(it => it.EndsWithOrdinal(expected),usedArguments: [new(nameof(expected), expectedExpression, expected)]);
}
