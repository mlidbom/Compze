using Compze.Utilities.SystemCE;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___String
{
   public static IAssertionContext<string> Contain(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.SatisfyInternal(it => it.ContainsCE(expected), expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<string> NotContain(this IAssertionContext<string> context, string unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      context.SatisfyInternal(it => !it.ContainsCE(unexpected), expressionValues: [new(unexpectedExpression, unexpected)]);

   public static IAssertionContext<string> StartWith(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.SatisfyInternal(it => it.StartsWithCE(expected), expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<string> EndWith(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.SatisfyInternal(it => it.EndsWithCE(expected),expressionValues: [new(expectedExpression, expected)]);

   public static IAssertionContext<string?> BeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(string.IsNullOrEmpty);

   public static IAssertionContext<string> NotBeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(it => !string.IsNullOrEmpty(it))!;

   public static IAssertionContext<string> NotBeNullOrWhiteSpace(this IAssertionContext<string?> context) =>
      context.SatisfyInternal(it => !string.IsNullOrWhiteSpace(it))!;
}
