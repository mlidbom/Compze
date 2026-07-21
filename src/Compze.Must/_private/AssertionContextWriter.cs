using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Must._private.Serialization;
using Newtonsoft.Json;

namespace Compze.Must._private;

static class AssertionContextWriter
{
   static string AssertionCode(this IAssertionContext context, string method, string? predicate = null) => $"{context.Expression}.Must().{method}({predicate})";

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

   public static string ExpressionValue<T>(this IAssertionContext<T> context) => context.ExpressionValue(context.Expression, context.Actual);
   public static string ExpressionValue(this IAssertionContext context, string expression, object? value)
   {
      expression = context.NormalizeExpressionIndentation(expression);
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
         if(toString == json) //A simple type for which Newtonsoft just outputs ToString()
         {
            if(expression.ReplaceOrdinal("\"", "") == toString) //an inline constant
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
         } else if(value is string aString)
         {
            return $"""
                    {expression} was a string with the value:
                    {AssertionContext.Separator}
                    {aString}
                    {AssertionContext.Separator}
                    """;
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

   public static string Diff(this IAssertionContext context, object expected, object actual, string? oldFileName = null, string? newFileName = null) =>
      context.Diff(Serialize(expected), Serialize(actual), oldFileName, newFileName);

   // ReSharper disable once UnusedParameter.Global
   public static string Diff(this IAssertionContext context, string expected, string actual, string? oldFileName = null, string? newFileName = null) =>
      $"""
       Diff:
       {AssertionContext.Separator}
       {DiffGenerator.CreateDiff(expected: expected, actual: actual, oldFileName: oldFileName, newFileName: newFileName)}
       {AssertionContext.Separator}
       """;

   static string Serialize(object? obj) => obj != null ? JsonConvert.SerializeObject(obj, TestingJsonSettings.AllMembers) : "null";

   // ReSharper disable once UnusedParameter.Global
   public static string NormalizeExpressionIndentation(this IAssertionContext context, string expression)
   {
      var lines = expression.Split(Environment.NewLine);
      if(lines.Length == 1) return expression;

      var minimumIndent = lines.Skip(1)
                               .Where(it => !string.IsNullOrWhiteSpace(it))
                               .Min(it => it.TakeWhile(char.IsWhiteSpace).Count());

      return string.Join(Environment.NewLine,
                         lines.Select((line, i) =>
                                         i == 0 ? line : line.Length > minimumIndent ? line.Substring(minimumIndent) : line));
   }
}
