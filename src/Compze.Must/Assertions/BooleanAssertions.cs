// ReSharper disable RedundantBoolCompare
// ReSharper disable InconsistentNaming

namespace Compze.Must.Assertions;

/// <summary>Boolean assertions.</summary>
public static class BooleanAssertions
{
   /// <summary>Asserts that the value is <see langword="true"/>.</summary>
   public static IAssertionContext<bool> BeTrue(this IAssertionContext<bool> context, string? message = null)
      => context.RunAssertion(it => it == true,
                              messageOverride: _ =>
                                 message ??
                                 $"""
                                  expected the expression: 
                                  {AssertionContext.Separator}
                                  {context.Expression.Indent()}
                                  {AssertionContext.Separator}
                                  to be true, but it was false
                                  """);

   /// <summary>Asserts that the value is <see langword="false"/>.</summary>
   public static IAssertionContext<bool> BeFalse(this IAssertionContext<bool> context, string? message = null)
      => context.RunAssertion(it => it == false,
                              messageOverride: _ =>
                                 message ??
                                 $"""
                                  expected the expression: 
                                  {AssertionContext.Separator}
                                  {context.Expression.Indent()}
                                  {AssertionContext.Separator}
                                  to be false, but it was true
                                  """);
}
