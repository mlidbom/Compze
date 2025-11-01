using Compze.Utilities.SystemCE;
// ReSharper disable RedundantBoolCompare

namespace Compze.Tests.Infrastructure.Fluent;

public static class BooleanBeTrueFalse
{
   public static Must<bool>? BeTrue(this Must<bool> must)
      => must.Satisfy(it => it == true,
                      messageOverride:() =>
                         $"""
                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be true, but it was false
                          """);


   public static Must<bool>? BeFalse(this Must<bool> must)
      => must.Satisfy(it => it == false,
                      messageOverride:() =>
                         $"""
                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be false, but it was true
                          """);
}
