using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Testing.Must.Serialization;
using Newtonsoft.Json;
using System.Linq;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.Must;

public static class AssertionContextWriter
{
   public static string AssertionCode(this IAssertionContext context, string method, string? predicate = null) => $"{context.Expression}.Must().{method}({predicate})";

   public static string FailingAssertionHeading(this IAssertionContext context, string method, string? predicate = null) =>
      $"""
       {AssertionContext.Separator}
       Failing assertion:
       {AssertionContext.Separator}
       {context.AssertionCode(method, predicate)}
       {AssertionContext.Separator}
       """;

   public static string FailingAssertionHeading(this IAssertionContext context, string callerName, ExpressionValue[]? usedArguments)
   {
      if(string.IsNullOrEmpty(callerName))
         return AssertionContext.RemoveLine;

      var argumentsExpressions = usedArguments != null && usedArguments.Any()
                                    ? usedArguments.Select(it => it.Expression).Join(", ")
                                    : "";

      return $"""
              {AssertionContext.Separator}
              Failing assertion:
              {AssertionContext.Separator}
              {context.Expression}.Must().{callerName}({argumentsExpressions})
              {AssertionContext.Separator}
              """;
   }

   public static string ArgumentValue(this IAssertionContext context, string expression, object? value)
   {
      if(value == null)
      {
         return $"""
                 {expression} was null:
                 {AssertionContext.Separator}
                 """;
      } else
      {
         var json = Serialize(value);
         var toString = value.ToString();
         if(toString == json) //A simple type for which Newtonsoft just outputs toString
         {
            if(expression == toString) //an inline constant
            {
               return $"""
                       {expression} was a {value.GetType().GetFullNameCompilable()}
                       {AssertionContext.Separator}
                       """;
            } else
            {
               return $"""
                       {expression} was a {value.GetType().GetFullNameCompilable()} with the value: {toString}
                       {AssertionContext.Separator}
                       """;
            }
         } else // A complex type
         {
            if(!value.GetType().Methods().HasMeaningfulToStringOverride())
            {
               return $"""
                       {expression} was a {value.GetType().GetFullNameCompilable()} with:
                       {AssertionContext.Separator}
                       JSON:
                       {AssertionContext.Separator}
                       {json}
                       {AssertionContext.Separator}
                       """;
            } else
            {
               return $"""
                       {expression} was a {value.GetType().GetFullNameCompilable()} with:
                       {AssertionContext.Separator}
                       ToString():
                       {AssertionContext.Separator}
                       {toString}
                       {AssertionContext.Separator}
                       JSON:
                       {AssertionContext.Separator}
                       {json}
                       {AssertionContext.Separator}
                       """;
            }
         }
      }
   }

   public static string Diff(this IAssertionContext context, string expected, string actual) => $"""
                                                                                                 Diff:
                                                                                                 {AssertionContext.Separator}
                                                                                                 {DiffGenerator.CreateDiff(expected: expected, actual: actual)}
                                                                                                 {AssertionContext.Separator}
                                                                                                 """;

   static string Serialize(object? obj) => obj != null ? JsonConvert.SerializeObject(obj, TestingJsonSettings.AllMembers) : "null";
}
