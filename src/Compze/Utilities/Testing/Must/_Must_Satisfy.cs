using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public record SatisfyCallInfo<T>(string PredicateExpression, Func<T, bool> Predicate, Func<T, string>? FailureMessage, string CallingMethod, IReadOnlyList<ExpressionValue>? UsedArguments) {}

public static class _Must_Satisfy
{
   public static IAssertionContext Satisfy(this IAssertionContext context,
                                           Func<object, bool> predicate,
                                           Func<object, string>? failureMessage = null,
                                           [CallerArgumentExpression(nameof(predicate))]
                                           string predicateExpression = null!) => context.Cast<object>().Satisfy(predicate, failureMessage: failureMessage, predicateExpression: predicateExpression);

   public static IAssertionContext<T> Satisfy<T>(this IAssertionContext<T> context,
                                                 Func<T, bool> predicate,
                                                 Func<T, string>? failureMessage = null,
                                                 [CallerArgumentExpression(nameof(predicate))]
                                                 string predicateExpression = null!)
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
                  {failureMessage?.Invoke(context.Actual)}
                  {AssertionContext.Separator}
                  """
               : AssertionContext.RemoveLine;
      }

      return context;
   }

   public static IAssertionContext SatisfyInternal(this IAssertionContext context,
                                                   Func<object, bool> predicate,
                                                   [CallerArgumentExpression(nameof(predicate))]
                                                   string predicateExpression = null!,
                                                   Func<SatisfyCallInfo<object>, string>? messageOverride = null,
                                                   Func<object, string>? failureMessage = null,
                                                   ExpressionValue[]? expressionValues = null,
                                                   [CallerMemberName] string caller = null!) => context.Cast<object>().SatisfyInternal(predicate, predicateExpression, messageOverride, failureMessage, expressionValues, caller);

   public static IAssertionContext<T> SatisfyInternal<T>(this IAssertionContext<T> context,
                                                         Func<T, bool> predicate,
                                                         [CallerArgumentExpression(nameof(predicate))]
                                                         string predicateExpression = null!,
                                                         Func<SatisfyCallInfo<T>, string>? messageOverride = null,
                                                         Func<T, string>? failureMessage = null,
                                                         ExpressionValue[]? expressionValues = null,
                                                         [CallerMemberName] string caller = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke(new SatisfyCallInfo<T>(predicateExpression, predicate, failureMessage, caller, expressionValues)));
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
                  {failureMessage?.Invoke(context.Actual)}
                  {AssertionContext.Separator}
                  """
               : AssertionContext.RemoveLine;
      }

      return context;
   }
}
