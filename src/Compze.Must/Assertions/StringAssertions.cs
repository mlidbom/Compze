using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Assertions;

/// <summary>String content assertions (ordinal comparison).</summary>
public static class StringAssertions
{
   /// <summary>Asserts that the string contains <paramref name="expected"/> (ordinal).</summary>
   public static IAssertionContext<string> Contain(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.RunAssertion(it => it.ContainsOrdinal(expected), expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the string does not contain <paramref name="unexpected"/> (ordinal).</summary>
   public static IAssertionContext<string> NotContain(this IAssertionContext<string> context, string unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      context.RunAssertion(it => !it.ContainsOrdinal(unexpected), expressionValues: [new(unexpectedExpression, unexpected)]);

   /// <summary>Asserts that the string starts with <paramref name="expected"/> (ordinal).</summary>
   public static IAssertionContext<string> StartWith(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.RunAssertion(it => it.StartsWithOrdinal(expected), expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the string ends with <paramref name="expected"/> (ordinal).</summary>
   public static IAssertionContext<string> EndWith(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.RunAssertion(it => it.EndsWithOrdinal(expected), expressionValues: [new(expectedExpression, expected)]);

   /// <summary>Asserts that the string is <see langword="null"/> or empty.</summary>
   public static IAssertionContext<string?> BeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.RunAssertion(string.IsNullOrEmpty);

   /// <summary>Asserts that the string is neither <see langword="null"/> nor empty, narrowing to non-nullable.</summary>
   public static IAssertionContext<string> NotBeNullOrEmpty(this IAssertionContext<string?> context) =>
      context.RunAssertion(it => !string.IsNullOrEmpty(it))!;

   /// <summary>Asserts that the string is neither <see langword="null"/>, empty, nor white-space, narrowing to non-nullable.</summary>
   public static IAssertionContext<string> NotBeNullOrWhiteSpace(this IAssertionContext<string?> context) =>
      context.RunAssertion(it => !string.IsNullOrWhiteSpace(it))!;
}
