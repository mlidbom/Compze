using System.Runtime.CompilerServices;
using Compze.Must._private;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>The universal predicate assertion: <see cref="Satisfy{T}(IAssertionContext{T}, System.Func{T, bool}, System.Func{T, string}, string)"/> checks any predicate and, on failure, renders the predicate expression together with the serialized state of the value.</summary>
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
}
