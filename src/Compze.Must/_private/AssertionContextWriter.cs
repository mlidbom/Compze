using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Must._private.Serialization;

namespace Compze.Must._private;

static class AssertionContextWriter
{
   public static readonly string NoDiffBecauseMustCouldNotSerializeBothValues =
      $"""
       Diff:
       {AssertionContext.Separator}
       None: Must could not serialize both values.
       {AssertionContext.Separator}
       """;

   extension<T>(IAssertionContext<T> @this)
   {
      public string ExpressionValue() => @this.ExpressionValue(@this.Expression, @this.Actual);
   }

   extension(IAssertionContext @this)
   {
      string AssertionCode(string method, string? predicate = null) => $"{@this.Expression}.Must().{method}({predicate})";

      public string FailingAssertionHeading(string method, string? predicate = null) =>
         $"""
          {AssertionContext.Separator}
          Failing assertion:
          {AssertionContext.Separator}
          {@this.AssertionCode(method, predicate)}
          {AssertionContext.Separator}
          """;

      public string FailingAssertionHeading(string callerName, ExpressionValue[]? usedArguments)
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
                 {@this.Expression}.Must().{callerName}({argumentsExpressions})
                 {AssertionContext.Separator}
                 """;
      }

      /// <summary>Each of <paramref name="expressionValues"/> as <see cref="ExpressionValue(IAssertionContext, string, object?)"/> shows it, or <see cref="AssertionContext.RemoveLine"/> when there are none.</summary>
      public string ExpressionValues(ExpressionValue[]? expressionValues) =>
         expressionValues == null || !expressionValues.Any()
            ? AssertionContext.RemoveLine
            : expressionValues.Select(it => @this.ExpressionValue(it.Expression, it.Value)).JoinLines();

      /// <summary>The failure message of the assertion <paramref name="callerName"/>, which needs a value to check, made on a <see langword="null"/> one.</summary>
      public string NullValueFailure(string callerName, ExpressionValue[]? expressionValues) =>
         $"""
          {@this.FailingAssertionHeading(callerName, expressionValues)}
          {@this.Expression} was null, so {callerName} had no value to check.
          {AssertionContext.Separator}
          {@this.ExpressionValues(expressionValues)}
          """.RemoveLinesWhere(it => it == AssertionContext.RemoveLine);

      public string ExpressionValue(string expression, object? value)
      {
         expression = @this.NormalizeExpressionIndentation(expression);
         if(value == null)
         {
            return $"""
                    {expression} was null:
                    {AssertionContext.Separator}
                    """;
         } else
         {
            var serialized = @this.SerializedForMessages(value);
            //review: A ToString() that throws still replaces the assertion's message, as a failing serialization did. Guarding it takes a
            //   catch of every exception, which the ban on defensive coding allows only on evidence that it happens; there is none yet.
            //   Say if it should be guarded anyway, as serialization is. (Claude, 2026-11-12)
            var toString = value.ToString() ?? "";
            if(serialized.Failure is {} failure)
            {
               return $"""
                       {expression} was a {value.GetType().GetFullNameCompilable()} that Must could not serialize:
                       {AssertionContext.Separator}
                       {failure.Describe()}
                       {AssertionContext.Separator}
                       ToString():
                       {AssertionContext.Separator}
                       {toString.ShownInAMessage(expression, "ToString.txt")}
                       {AssertionContext.Separator}
                       """;
            }

            var json = serialized.Json;
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
                       {aString.ShownInAMessage(expression, "txt")}
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
                          {json.ShownInAMessage(expression, "json")}
                          {AssertionContext.Separator}
                          """;
               } else
               {
                  return $"""
                          {expression} was a {value.GetType().GetFullNameCompilable()} with:
                          {AssertionContext.Separator}
                          ToString():
                          {AssertionContext.Separator}
                          {toString.ShownInAMessage(expression, "ToString.txt")}
                          {AssertionContext.Separator}
                          JSON:
                          {AssertionContext.Separator}
                          {json.ShownInAMessage(expression, "json")}
                          {AssertionContext.Separator}
                          """;
               }
            }
         }
      }

      public string Diff(object? expected, object? actual, string? oldFileName = null, string? newFileName = null)
      {
         var expectedSerialized = @this.SerializedForMessages(expected);
         var actualSerialized = @this.SerializedForMessages(actual);
         return expectedSerialized.Succeeded && actualSerialized.Succeeded
                   ? @this.Diff(expectedSerialized.Json, actualSerialized.Json, oldFileName, newFileName)
                   : NoDiffBecauseMustCouldNotSerializeBothValues;
      }

      public string Diff(string expected, string actual, string? oldFileName = null, string? newFileName = null) =>
         $"""
          Diff:
          {AssertionContext.Separator}
          {DiffGenerator.CreateDiff(expected: expected, actual: actual, oldFileName: oldFileName, newFileName: newFileName).ShownInAMessage($"{@this.Expression}-diff", "diff")}
          {AssertionContext.Separator}
          """;

      /// <summary>What <paramref name="serialized"/>'s failure was, or <see cref="AssertionContext.RemoveLine"/> when it has none.</summary>
      public string WhyItCouldNotBeSerialized(string expression, SerializedValue serialized) =>
         serialized.Failure is {} failure
            ? $"""
               {@this.NormalizeExpressionIndentation(expression)} could not be serialized:
               {AssertionContext.Separator}
               {failure.Describe()}
               {AssertionContext.Separator}
               """
            : AssertionContext.RemoveLine;

      /// <summary>The value serialized as failure messages show values: every member, by this assertion chain's serialization rules.</summary>
      public SerializedValue SerializedForMessages(object? value) =>
         SerializedValue.Of(value, MemberVisibility.AllMembers, @this.SerializationRules);

      public string NormalizeExpressionIndentation(string expression)
      {
         var lines = expression.Split(Environment.NewLine);
         if(lines.Length == 1) return expression;

         var minimumIndent = lines.Skip(1)
                                  .Where(it => !string.IsNullOrWhiteSpace(it))
                                  .Min(it => it.TakeWhile(char.IsWhiteSpace).Count());

         return string.Join(Environment.NewLine,
                            lines.Select((line, i) =>
                                            i == 0 ? line : line.Length > minimumIndent ? line[minimumIndent..] : line));
      }
   }
}
