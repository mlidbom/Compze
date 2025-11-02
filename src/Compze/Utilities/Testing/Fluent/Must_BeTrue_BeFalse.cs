using Compze.Utilities.SystemCE;

// ReSharper disable RedundantBoolCompare
// ReSharper disable InconsistentNaming
namespace Compze.Utilities.Testing.Fluent;

public static class Must_BeTrue_BeFalse
{
   public static IMust<bool>? BeTrue(this IMust<bool> must)
      => must.SatisfyInternal(it => it == true,
                      messageOverride: _ =>
                         $"""
                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be true, but it was false
                          """);


   public static IMust<bool>? BeFalse(this IMust<bool> must)
      => must.SatisfyInternal(it => it == false,
                      messageOverride:_ =>
                         $"""
                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to be false, but it was true
                          """);
}
