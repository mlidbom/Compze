using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must.Serialization;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public record SatisfyCallInfo<T>(string PredicateExpression, Func<T, bool> Predicate, Func<T, string>? FailureMessage, IReadOnlyList<ExpressionValue>? UsedArguments) {}

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
             {context.ArgumentValue(context.Expression, context.Actual)}
             """.Split(Environment.NewLine)
                .Where(it => it != AssertionContext.RemoveLine)
                .JoinLines();

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
                                       ExpressionValue[]? expressions = null,
                                       [CallerMemberName] string caller = null!) => context.Cast<object>().SatisfyInternal(predicate, predicateExpression, messageOverride, failureMessage, expressions, caller);

   public static IAssertionContext<T> SatisfyInternal<T>(this IAssertionContext<T> context,
                                             Func<T, bool> predicate,
                                             [CallerArgumentExpression(nameof(predicate))]
                                             string predicateExpression = null!,
                                             Func<SatisfyCallInfo<T>, string>? messageOverride = null,
                                             Func<T, string>? failureMessage = null,
                                             ExpressionValue[]? expressions = null,
                                             [CallerMemberName] string? caller = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke(new SatisfyCallInfo<T>(predicateExpression, predicate, failureMessage, expressions)));
         }

         var message = $"""
             {context.FailingAssertionHeading(caller!, expressions)}
             {CustomFailureMessage()}
             {context.ArgumentValue(context.Expression, context.Actual)}
             {ExpressionValues()}
             """.Split(Environment.NewLine)
                .Where(it => it != AssertionContext.RemoveLine)
                .JoinLines();

         throw new AssertionFailedException(message);

         string ExpressionValues()
         {
            if(expressions == null || !expressions.Any())
               return AssertionContext.RemoveLine;

            return $"""
                    {expressions.Select(it => context.ArgumentValue(it.Expression, it.Value)).JoinLines()}
                    """;
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
