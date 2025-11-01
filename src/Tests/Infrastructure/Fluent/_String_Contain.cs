using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class StringContain
{
   public static Must<string> Contain(this Must<string> must, string expected)
      => must.Satisfy(it => it.ContainsOrdinal(expected),
                      () =>
                         $"""
                          {must.Separator}
                          expected the string produced by the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to contain the string:
                          {must.Separator}
                          {expected}
                          {must.Separator}
                          but it was:
                          {must.Separator}
                          {must.Actual}
                          {must.Separator}
                          """);
}
