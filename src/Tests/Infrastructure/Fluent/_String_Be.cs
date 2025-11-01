using Compze.Utilities.SystemCE;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class StringBe
{
   public static Must<string>? Be(this Must<string> must, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.Satisfy(it => Equals(it, expected),
                      messageOverride: _ =>
                         $"""""
                          {must.Separator}
                          the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          did not result in the expected string, producing the diff
                          {must.Separator}
                          {DiffGenerator.CreateDiff(expected, must.Actual)}
                          {must.Separator}
                          Actual was:
                          {must.Separator}
                          {must.Actual}
                          {must.Separator}
                          Expected was:
                          {must.Separator}
                          {expected}
                          {must.Separator}
                          """"");
}