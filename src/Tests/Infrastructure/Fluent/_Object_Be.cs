using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectBe
{
   public static IMust<TValue>? Be<TValue>(this IMust<TValue> must, TValue expected)
      => must.Satisfy(it => Equals(it, expected),
                      () =>
                         $"""

                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to Be:
                          {must.Separator}
                          {expected}
                          {must.Separator}
                          but it was:
                          {must.Separator}
                          {must.Actual}
                          {must.Separator}
                          """);
}
