using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must_Be_string
{
   public static IAssertionContext<string> Be(this IAssertionContext<string> context, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => context.SatisfyInternal(it => Equals(it, expected),
                      messageOverride: _ =>
                         $"""""
                          {AssertionContext.Separator}
                          the expression: 
                          {AssertionContext.Separator}
                          {context.Expression.Indent()}
                          {AssertionContext.Separator}
                          did not result in the expected string, producing the diff
                          {AssertionContext.Separator}
                          {DiffGenerator.CreateDiff(expected, context.Actual)}
                          {AssertionContext.Separator}
                          Actual was:
                          {AssertionContext.Separator}
                          {context.Actual}
                          {AssertionContext.Separator}
                          Expected was:
                          {AssertionContext.Separator}
                          {expected}
                          {AssertionContext.Separator}
                          """"");
}