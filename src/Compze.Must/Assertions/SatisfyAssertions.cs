using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>Details of a failing assertion, passed to a custom failure-message builder.</summary>
/// <param name="PredicateExpression">The source text of the predicate that failed.</param>
/// <param name="FailureMessage">An optional function producing a custom failure message from the actual value.</param>
/// <param name="CallingMethod">The name of the assertion method that performed the check.</param>
/// <param name="UsedArguments">The expression/value pairs of the arguments involved, for rendering.</param>
public record SatisfyCallInfo<T>(string PredicateExpression, Func<T, string>? FailureMessage, string CallingMethod, IReadOnlyList<ExpressionValue>? UsedArguments);
// ReSharper restore NotAccessedPositionalProperty.Global

/// <summary>Predicate-based assertions: the one-off <see cref="Satisfy{T}(IAssertionContext{T}, System.Func{T, bool}, System.Func{T, string}, string)"/>, and <see cref="RunAssertion{T}(IAssertionContext{T}, System.Func{T, bool}, string, System.Func{SatisfyCallInfo{T}, string}, System.Func{T, string}, ExpressionValue[], string)"/> — the primitive every other assertion is built on.</summary>
public static class SatisfyAssertions
{
   /// <summary>Asserts that the value satisfies <paramref name="predicate"/>.</summary>
   public static IAssertionContext Satisfy(this IAssertionContext context,
                                           Func<object, bool> predicate,
                                           Func<object, string>? failureMessage = null,
                                           [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!) => context.Cast<object>().Satisfy(predicate, failureMessage: failureMessage, predicateExpression: predicateExpression);

   /// <summary>Asserts that the value satisfies <paramref name="predicate"/>, with an optional custom <paramref name="failureMessage"/>.</summary>
   public static IAssertionContext<T> Satisfy<T>(this IAssertionContext<T> context,
                                                 Func<T, bool> predicate,
                                                 Func<T, string>? failureMessage = null,
                                                 [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!)
   {
      if(!predicate(context.Actual))
      {
         var message = $"""
                        {context.FailingAssertionHeading(nameof(Satisfy), predicateExpression)}
                        {CustomFailureMessage()}
                        {context.ExpressionValue()}
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

   internal static IAssertionContext RunAssertion(this IAssertionContext context,
                                                  Func<object, bool> predicate,
                                                  [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!,
                                                  Func<SatisfyCallInfo<object>, string>? messageOverride = null,
                                                  Func<object, string>? failureMessage = null,
                                                  ExpressionValue[]? expressionValues = null,
                                                  [CallerMemberName] string caller = null!) => context.Cast<object>().RunAssertion(predicate, predicateExpression, messageOverride, failureMessage, expressionValues, caller);

   /// <summary>
   /// The extension point for authoring custom assertions: write an extension method on <see cref="IAssertionContext{T}"/> and
   /// call this. It checks <paramref name="predicate"/> and, on failure, throws an <see cref="AssertionFailedException"/> whose
   /// heading is automatically labelled with the calling assertion method's name (captured from the call site) and renders the
   /// supplied <paramref name="expressionValues"/>. Unlike <see cref="Satisfy{T}(IAssertionContext{T}, System.Func{T, bool}, System.Func{T, string}, string)"/>,
   /// which always labels failures "Satisfy", this surfaces your own assertion's name.
   /// </summary>
   public static IAssertionContext<T> RunAssertion<T>(this IAssertionContext<T> context,
                                                      Func<T, bool> predicate,
                                                      [CallerArgumentExpression(nameof(predicate))] string predicateExpression = null!,
                                                      Func<SatisfyCallInfo<T>, string>? messageOverride = null,
                                                      Func<T, string>? failureMessage = null,
                                                      ExpressionValue[]? expressionValues = null,
                                                      [CallerMemberName] string caller = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke(new SatisfyCallInfo<T>(predicateExpression, failureMessage, caller, expressionValues)));
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
