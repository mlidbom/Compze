using System.Runtime.CompilerServices;
using Compze.Must._private;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>
/// The primitives every built-in assertion is built on, and the extension points for authoring your own: <see cref="RunAssertion{T}"/>,<br/>
/// and <see cref="RunAssertionThatANullValueFails{T}"/> for an assertion that needs a value to check.
/// </summary>
public static class AssertionPrimitive
{
   internal static IAssertionContext RunAssertion(this IAssertionContext context,
                                                  Func<object, bool> predicate,
                                                  [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!,
                                                  Func<AssertionCallInfo<object>, string>? messageOverride = null,
                                                  Func<object, string>? failureMessage = null,
                                                  ExpressionValue[]? expressionValues = null,
                                                  [CallerMemberName] string caller = null!) => context.Cast<object>().RunAssertion(predicate, predicateExpression, messageOverride, failureMessage, expressionValues, caller);

   /// <summary>
   /// The extension point for authoring custom assertions: write an extension method on <see cref="IAssertionContext{T}"/> and
   /// call this. It checks <paramref name="predicate"/> and, on failure, throws an <see cref="AssertionFailedException"/> whose
   /// heading is automatically labelled with the calling assertion method's name (captured from the call site) and renders the
   /// supplied <paramref name="expressionValues"/>. Unlike <see cref="SatisfyAssertions"/>'s <c>Satisfy</c>, which always labels
   /// failures "Satisfy", this surfaces your own assertion's name.
   /// </summary>
   public static IAssertionContext<T> RunAssertion<T>(this IAssertionContext<T> context,
                                                      Func<T, bool> predicate,
                                                      [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!,
                                                      Func<AssertionCallInfo<T>, string>? messageOverride = null,
                                                      Func<T, string>? failureMessage = null,
                                                      ExpressionValue[]? expressionValues = null,
                                                      [CallerMemberName] string caller = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke(new AssertionCallInfo<T>(predicateExpression, failureMessage, caller, expressionValues)));
         }

         var message = $"""
                        {context.FailingAssertionHeading(caller, expressionValues)}
                        {CustomFailureMessage()}
                        {context.ExpressionValue()}
                        {context.ExpressionValues(expressionValues)}
                        """.RemoveLinesWhere(it => it == AssertionContext.RemoveLine);

         throw new AssertionFailedException(message);

         string CustomFailureMessage() =>
            failureMessage != null
               ? $"""
                  {failureMessage(context.Actual)}
                  {AssertionContext.Separator}
                  """
               : AssertionContext.RemoveLine;
      }

      return context;
   }

   /// <summary>
   /// The extension point for authoring a custom assertion that needs a value to check, such as one about a string's text:<br/>
   /// <see cref="RunAssertion{T}"/>, except that it fails a <see langword="null"/> value itself, before <paramref name="predicate"/> sees it, and<br/>
   /// returns the context narrowed to the non-nullable type.
   /// </summary>
   /// <remarks>
   /// It accepts a subject declared nullable, so the custom assertion compiles without a nullable warning on one, and its failure<br/>
   /// message for a <see langword="null"/> value says that the assertion had no value to check. Every built-in assertion that needs<br/>
   /// a value, such as <see cref="StringAssertions.StartWith"/>, is built on it.<br/>
   /// <paramref name="predicate"/>, <paramref name="messageOverride"/> and <paramref name="failureMessage"/> only ever see a value.
   /// </remarks>
   public static IAssertionContext<T> RunAssertionThatANullValueFails<T>(this IAssertionContext<T?> context,
                                                                         Func<T, bool> predicate,
                                                                         [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!,
                                                                         Func<AssertionCallInfo<T>, string>? messageOverride = null,
                                                                         Func<T, string>? failureMessage = null,
                                                                         ExpressionValue[]? expressionValues = null,
                                                                         [CallerMemberName] string caller = null!)
   {
      if(context.Actual is null)
         throw new AssertionFailedException(context.NullValueFailure(caller, expressionValues));

      return context.Cast<T>().RunAssertion(predicate, predicateExpression, messageOverride, failureMessage, expressionValues, caller);
   }
}
