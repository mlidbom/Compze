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
   public static IMust Satisfy(this IMust must,
                               Func<object, bool> predicate,
                               Func<object, string>? failureMessage = null,
                               [CallerArgumentExpression(nameof(predicate))]
                               string predicateExpression = null!) => must.Cast<object>().Satisfy(predicate, failureMessage: failureMessage, predicateExpression: predicateExpression);

   public static IMust<T> Satisfy<T>(this IMust<T> context,
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
                .Where(it => it != Must.RemoveLine)
                .JoinLines();

         throw new AssertionFailedException(message);

         string CustomFailureMessage() =>
            failureMessage != null
               ? $"""
                  {failureMessage?.Invoke(context.Actual)}
                  {context.Separator}
                  """
               : Must.RemoveLine;
      }

      return context;
   }

   public static IMust SatisfyInternal(this IMust must,
                                       Func<object, bool> predicate,
                                       [CallerArgumentExpression(nameof(predicate))]
                                       string predicateExpression = null!,
                                       Func<SatisfyCallInfo<object>, string>? messageOverride = null,
                                       Func<object, string>? failureMessage = null,
                                       AssertionArgumentInfo[]? usedArguments = null,
                                       [CallerMemberName] string callerName = null!) => must.Cast<object>().SatisfyInternal(predicate, predicateExpression, messageOverride, failureMessage, usedArguments, callerName);

   public static IMust<T> SatisfyInternal<T>(this IMust<T> context,
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

         var parameterName = ExtractParameterName(predicateExpression);

         var message = $"""
             {context.Separator}
             Failing assertion:
             {context.Separator}
             {AssertionMethodCall()}
             {ArgumentDescription(parameterName, context.Expression)}
             {DisplayUsedArgumentsDefinitions()}
             failed to Satisfy:
             {context.Separator}
             {predicateExpression.Indent()}
             {context.Separator}
             {CustomFailureMessage()}
             {ArgumentValue(parameterName, context.Actual)}
             {DisplayUsedArgumentsValues()}
             """.Split(Environment.NewLine)
                .Where(it => it != Must.RemoveLine)
                .JoinLines();

         throw new AssertionFailedException(message);

         string DisplayUsedArgumentsDefinitions()
         {
            if(usedArguments == null || !usedArguments.Any())
               return Must.RemoveLine;

            var stringBuilder = new StringBuilder();
            return $"""
                    {usedArguments.Select(it => ArgumentDescription(it.Name, it.Expression)).JoinLines()}
                    """;
         }

         string DisplayUsedArgumentsValues()
         {
            if(usedArguments == null || !usedArguments.Any())
               return Must.RemoveLine;

            var stringBuilder = new StringBuilder();
            return $"""
                    {usedArguments.Select(it => ArgumentValue(it.Name, it.Value)).JoinLines()}
                    """;
         }

         static string ArgumentDescription(string name, string expression) =>
            $"""
             "{name}" defined by:
             {Must.Separator}
             {expression.Indent()}
             {Must.Separator}
             """;

         string CustomFailureMessage() =>
            failureMessage != null
               ? $"""
                  {failureMessage?.Invoke(context.Actual) ?? "but it did not"}
                  {context.Separator}
                  """
               : Must.RemoveLine;

         string AssertionMethodCall()
         {
            if(string.IsNullOrEmpty(callerName))
               return Must.RemoveLine;

            var arguments = usedArguments != null && usedArguments.Any()
                               ? usedArguments.Select(it => it.Expression).Join(", ")
                               : "";

            return $"""
                    {context.Expression}.Must().{callerName}({arguments})
                    {context.Separator}
                    """;
         }
      }

      return context;
   }

   static string ArgumentValue(string name, object? value)
   {
      return $"""
              "{name}" was:
              {Must.Separator}
              ToString():
              {Must.Separator}
              {value?.ToString() ?? "null"}
              {Must.Separator}
              JSON:
              {Must.Separator}
              {Serialize(value)}
              {Must.Separator}
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
