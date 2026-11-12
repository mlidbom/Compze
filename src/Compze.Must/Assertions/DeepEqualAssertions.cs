using System.Runtime.CompilerServices;
using Compze.Must._private;
using Compze.Must._private.Serialization;

// ReSharper disable InconsistentNaming
namespace Compze.Must;

/// <summary>Deep structural-equality assertions that compare two values by serializing both and diffing the results.</summary>
/// <remarks>
/// The method decides which members are compared: all of them, internal and public ones, or public ones. The Must call's
/// <see cref="ISerializationConfig{TValue}"/>, and the global rules of <see cref="MustSerialization"/>, decide what is
/// left out or written differently.<br/>
/// When either value cannot be serialized, the assertion fails, saying why, instead of comparing what could be serialized.
/// </remarks>
public static class DeepEqualAssertions
{
   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing all members (public, internal, and private). Equivalent to <see cref="DeepEqualPrivate{TValue}"/>.</summary>
   public static IAssertionContext<TValue> DeepEqual<TValue>(this IAssertionContext<TValue> context,
                                                             TValue expected,
                                                             [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.DeepEqualPrivate(expected, expectedExpression);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing all members including private ones.</summary>
   public static IAssertionContext<TValue> DeepEqualPrivate<TValue>(this IAssertionContext<TValue> context,
                                                                    TValue expected,
                                                                    [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      DeepEqualCore(context, expected, expectedExpression, MemberVisibility.AllMembers);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing internal and public members.</summary>
   public static IAssertionContext<TValue> DeepEqualInternal<TValue>(this IAssertionContext<TValue> context,
                                                                     TValue expected,
                                                                     [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => DeepEqualCore(context, expected, expectedExpression, MemberVisibility.InternalAndPublicMembers);

   /// <summary>Asserts deep structural equality with <paramref name="expected"/>, comparing only public members.</summary>
   public static IAssertionContext<TValue> DeepEqualPublic<TValue>(this IAssertionContext<TValue> context,
                                                                   TValue expected,
                                                                   [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => DeepEqualCore(context, expected, expectedExpression, MemberVisibility.PublicMembers);

   static IAssertionContext<TValue> DeepEqualCore<TValue>(IAssertionContext<TValue> context,
                                                          TValue expected,
                                                          string expectedExpression,
                                                          MemberVisibility visibility)
   {
      var actualSerialized = SerializedValue.Of(context.Actual, visibility, context.SerializationRules);
      var expectedSerialized = SerializedValue.Of(expected, visibility, context.SerializationRules);

      if(!actualSerialized.Succeeded || !expectedSerialized.Succeeded)
      {
         throw new AssertionFailedException($"""
                                             {context.FailingAssertionHeading(nameof(DeepEqual), [new ExpressionValue(expectedExpression, expected)])}
                                             DeepEqual could not compare the values, because Must could not serialize them both.
                                             {AssertionContext.Separator}
                                             {context.WhyItCouldNotBeSerialized(context.Expression, actualSerialized)}
                                             {context.WhyItCouldNotBeSerialized(expectedExpression, expectedSerialized)}
                                             """.RemoveLinesWhere(it => it == AssertionContext.RemoveLine));
      }

      return context.RunAssertion(_ => actualSerialized.Json == expectedSerialized.Json,
                                  messageOverride: _ =>
                                     $"""
                                      {context.FailingAssertionHeading(nameof(DeepEqual), [new ExpressionValue(expectedExpression, expected)])}
                                      {context.Diff(expectedSerialized.Json, actualSerialized.Json)}
                                      {context.ExpressionValue()}
                                      {context.ExpressionValue(expectedExpression, expected)}
                                      """);
   }
}
