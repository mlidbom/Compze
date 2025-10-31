using Compze.Utilities.SystemCE;
// ReSharper disable RedundantBoolCompare

namespace Compze.Tests.Infrastructure.Fluent;

public static class BooleanBeTrueFalse
{
   public static IMust<bool>? BeTrue(this IMust<bool> must)
      => must.Satisfy(it => it == true,
                      () =>
                         $"""

                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be true
                          """);


   public static IMust<bool>? BeFalse(this IMust<bool> must)
      => must.Satisfy(it => it == false,
                      () =>
                         $"""

                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be false
                          """);
}
