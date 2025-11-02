using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Fluent.Serialization;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.Fluent;

public record SatisfyCallInfo<T>(string PredicateExpression, Func<T, bool> Predicate, Func<T, string>? FailureMessage, IReadOnlyList<AssertionArgumentInfo>? UsedArguments)
{

}

public static class _Must_Satisfy
{
   const string RemoveLine = nameof(RemoveLine);

   public static IMust Satisfy(this IMust must,
                              Func<object, bool> predicate,
                              [CallerArgumentExpression(nameof(predicate))]
                              string predicateExpression = null!,
                              Func<SatisfyCallInfo<object>, string>? messageOverride = null,
                              Func<object, string>? failureMessage = null,
                              AssertionArgumentInfo[]? usedArguments = null) => must.Cast<object>().Satisfy(predicate, predicateExpression, messageOverride, failureMessage, usedArguments);

   public static IMust<T> Satisfy<T>(this IMust<T> context,
                                    Func<T, bool> predicate,
                                    [CallerArgumentExpression(nameof(predicate))]
                                    string predicateExpression = null!,
                                    Func<SatisfyCallInfo<T>, string>? messageOverride = null,
                                    Func<T, string>? failureMessage = null,
                                    AssertionArgumentInfo[]? usedArguments = null)
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
                .Where(it => it != RemoveLine)
                .JoinLines();

         throw new AssertionFailedException(message);

         string DisplayUsedArgumentsDefinitions()
         {
            if(usedArguments == null || !usedArguments.Any())
               return RemoveLine;

            var stringBuilder = new StringBuilder();
            return $"""
                    {usedArguments.Select(it => ArgumentDescription(it.Name, it.Expression)).JoinLines()}
                    """;
         }

         string DisplayUsedArgumentsValues()
         {
            if(usedArguments == null || !usedArguments.Any())
               return RemoveLine;

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
            failureMessage != null ? $"""
                                      {failureMessage?.Invoke(context.Actual) ?? "but it did not"}
                                      {context.Separator}
                                      """ : RemoveLine;

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
      }

      return context;
   }

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
