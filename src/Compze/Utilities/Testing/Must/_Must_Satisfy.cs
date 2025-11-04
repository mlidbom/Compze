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

public record SatisfyCallInfo<T>(string PredicateExpression, Func<T, bool> Predicate, Func<T, string>? FailureMessage, IReadOnlyList<AssertionArgumentInfo>? UsedArguments) {}

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
             {ArgumentValue(context.Expression, context.Actual)}
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
                                       AssertionArgumentInfo[]? usedArguments = null,
                                       [CallerMemberName] string callerName = null!) => context.Cast<object>().SatisfyInternal(predicate, predicateExpression, messageOverride, failureMessage, usedArguments, callerName);

   public static IAssertionContext<T> SatisfyInternal<T>(this IAssertionContext<T> context,
                                             Func<T, bool> predicate,
                                             [CallerArgumentExpression(nameof(predicate))]
                                             string predicateExpression = null!,
                                             Func<SatisfyCallInfo<T>, string>? messageOverride = null,
                                             Func<T, string>? failureMessage = null,
                                             AssertionArgumentInfo[]? usedArguments = null,
                                             [CallerMemberName] string? callerName = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke(new SatisfyCallInfo<T>(predicateExpression, predicate, failureMessage, usedArguments)));
         }

         var message = $"""
             {context.FailingAssertionHeading(callerName!, predicateExpression, usedArguments)}
             {CustomFailureMessage()}
             {ArgumentValue(context.Expression, context.Actual)}
             {DisplayUsedArgumentsValues()}
             """.Split(Environment.NewLine)
                .Where(it => it != AssertionContext.RemoveLine)
                .JoinLines();

         throw new AssertionFailedException(message);

         string DisplayUsedArgumentsValues()
         {
            if(usedArguments == null || !usedArguments.Any())
               return AssertionContext.RemoveLine;

            var stringBuilder = new StringBuilder();
            return $"""
                    {usedArguments.Select(it => ArgumentValue(it.Expression, it.Value)).JoinLines()}
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

   static string ArgumentValue(string expression, object? value)
   {
      return $"""
              {expression} was:
              {AssertionContext.Separator}
              ToString():
              {AssertionContext.Separator}
              {value?.ToString() ?? "null"}
              {AssertionContext.Separator}
              JSON:
              {AssertionContext.Separator}
              {Serialize(value)}
              {AssertionContext.Separator}
              """;
   }

   static string Serialize(object? obj) => obj != null ? JsonConvert.SerializeObject(obj, TestingJsonSettings.AllMembers) : "null";

   static string ExtractParameterName(string lambdaExpression)
   {
      // Handle expressions like "it => it.Value > 5" or "x => x > 0"
      var arrowIndex = lambdaExpression.IndexOf("=>", StringComparison.Ordinal);
      if(arrowIndex == -1)
         return "it"; // Fallback to "it" if we can't parse

      var parameterPart = lambdaExpression[..arrowIndex].Trim();

      // Remove parentheses if present: "(it)" => "it"
      if(parameterPart.StartsWith('(') && parameterPart.EndsWith(')'))
         parameterPart = parameterPart[1..^1].Trim();

      return parameterPart;
   }
}
