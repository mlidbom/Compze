using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Assertions;

/// <summary>The primitive every built-in assertion is built on, and the extension point for authoring your own: <see cref="RunAssertion{T}"/>.</summary>
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
                        {ExpressionValues()}
                        """.RemoveLinesWhere(it => it == AssertionContext.RemoveLine);

         throw new AssertionFailedException(message);

         string ExpressionValues()
         {
            if(expressionValues == null || !expressionValues.Any())
               return AssertionContext.RemoveLine;

            return expressionValues.Select(it => context.ExpressionValue(it.Expression, it.Value)).JoinLines();
         }

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
}
