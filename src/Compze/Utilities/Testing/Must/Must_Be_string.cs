using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must_Be_string
{
   public static IAssertionContext<string> Be(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => context.SatisfyInternal(it => Equals(it, expected),
                      messageOverride: _ =>
                         $"""
                          {context.FailingAssertionHeading(nameof(Be), [new(expectedExpression, expected)])}
                          {context.Diff(expected, context.Actual)}
                          {context.ExpressionValue()}
                          {context.ExpressionValue(expectedExpression, expected)}
                          """);
}