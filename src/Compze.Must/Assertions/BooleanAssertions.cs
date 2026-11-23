// ReSharper disable RedundantBoolCompare
// ReSharper disable InconsistentNaming

using Compze.Must._private;

namespace Compze.Must;

/// <summary>
/// Boolean assertions.<br/>
/// For a check that no Compze.Must assertion makes, use <see cref="SatisfyAssertions.Satisfy{T}(IAssertionContext{T}, Func{T, bool}, Func{T, string}, string)"/>:<br/>
/// it takes any predicate, and its failure shows the predicate and the value.
/// </summary>
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
