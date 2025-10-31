using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class StringContain
{
   public static IMust<string> Contain(this IMust<string> must, string expected)
      => must.Satisfy(it => it.ContainsOrdinal(expected),
                      () =>
                         $"""
                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to Contain:
                          {must.Separator}
                          {expected}
                          {must.Separator}
                          but it was:
                          {must.Separator}
                          {must.Actual}
                          {must.Separator}
                          """);
}
