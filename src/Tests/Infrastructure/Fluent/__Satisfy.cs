using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Satisfy
{
   const string RemoveLine = nameof(RemoveLine);

   public static Must<T> Satisfy<T>(this Must<T> context,
                                    Func<T, bool> predicate,
                                    Func<string>? messageOverride = null,
                                    [CallerArgumentExpression(nameof(predicate))]
                                    string predicateExpression = null!,
                                    Func<T, string>? failureMessage = null,
                                    IReadOnlyList<AssertionArgumentInfo>? usedArguments = null)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke());
         }

         var message = $"""
             {context.Separator}
             expected the object returned by the expression:
             {context.Separator}
             {context.Expression.Indent()}
             {context.Separator}
             to Satisfy:
             {context.Separator}
             {predicateExpression.Indent()}
             {context.Separator}
             {DisplayUsedArgumentsDefinitions()}
             {failureMessage?.Invoke(context.Actual) ?? "but it did not"}
             {context.Separator}
             The value of: 
             {context.Expression.Indent()}
             Was:
             {context.Separator}
             ToString():
             {context.Separator}
             {context.Actual?.ToString() ?? "null"}
             {context.Separator}
             JSON:
             {context.Separator}
             {JsonConvert.SerializeObject(context.Actual, TestingJsonSettings.AllMembers)}
             {context.Separator}
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
                    Where:
                    {usedArguments.Select(ArgumentDescription).JoinLines()}
                    """;
         }

         string DisplayUsedArgumentsValues()
         {
            if(usedArguments == null || !usedArguments.Any())
               return RemoveLine;

            var stringBuilder = new StringBuilder();
            return $"""
                    Where:
                    {usedArguments.Select(ArgumentValueDisplay).JoinLines()}
                    """;
         }

         static string ArgumentDescription(AssertionArgumentInfo arg) =>
            $"""
             {arg.Name} was:
             {arg.Expression.Indent()}
             {Must.Separator}
             """;

         static string ArgumentValueDisplay(AssertionArgumentInfo context)
         {
            var something = context.Value != null ? JsonConvert.SerializeObject(context.Value, TestingJsonSettings.AllMembers) : "null";
            return $"""
                    The value of: 
                    {context.Expression.Indent()}
                    Was:
                    {Must.Separator}
                    ToString():
                    {Must.Separator}
                    {context.Value?.ToString() ?? "null"}
                    {Must.Separator}
                    JSON:
                    {Must.Separator}
                    {Serialize(context.Value)}
                    {Must.Separator}
                    """;
         }

         static string Serialize(object? obj) => obj != null ? JsonConvert.SerializeObject(obj, TestingJsonSettings.AllMembers) : "null";
      }

      return context;
   }
}
