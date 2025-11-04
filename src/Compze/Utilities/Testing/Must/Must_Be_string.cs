using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must_Be_string
{
   public static IMust<string> Be(this IMust<string> must, string expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.SatisfyInternal(it => Equals(it, expected),
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