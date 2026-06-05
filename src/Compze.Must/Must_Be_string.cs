using System.Runtime.CompilerServices;

namespace Compze.Must;
// ReSharper disable InconsistentNaming

/// <summary>String equality assertion.</summary>
public static class Must_Be_string
{
   /// <summary>Asserts that the string equals <paramref name="expected"/>, rendering a diff on failure.</summary>
   public static IAssertionContext<string> Be(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => context.RunAssertion(it => Equals(it, expected),
                      messageOverride: _ =>
                         $"""
                          {context.FailingAssertionHeading(nameof(Be), [new(expectedExpression, expected)])}
                          {context.Diff(expected, context.Actual)}
                          {context.ExpressionValue()}
                          {context.ExpressionValue(expectedExpression, expected)}
                          """);
}
