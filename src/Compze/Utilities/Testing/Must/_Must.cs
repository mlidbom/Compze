using System;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Testing.Must.Serialization;
using Newtonsoft.Json;

#pragma warning disable CA1033 // The compiler is unhappy about the explicit interface implementation below

namespace Compze.Utilities.Testing.Must;

public interface IAssertionContext
{
   string Expression { get; }
   IAssertionContext<T> Cast<T>();
   string NormalizeExpressionIndentation(string expression);

   string AssertionCode(string method, string? predicate = null) => $"{Expression}.Must().{method}({predicate})";

   string FailingAssertionHeading(string method, string? predicate = null) =>
      $"""
       {AssertionContext.Separator}
       Failing assertion:
       {AssertionContext.Separator}
       {AssertionCode(method, predicate)}
       {AssertionContext.Separator}
       """;

   string FailingAssertionHeading(string callerName, ExpressionValue[]? usedArguments)
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
              {Expression}.Must().{callerName}({argumentsExpressions})
              {AssertionContext.Separator}
              """;
   }

   string ArgumentValue(string expression, object? value)
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
            if(toString == value.GetType().FullName)
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

   static string Serialize(object? obj) => obj != null ? JsonConvert.SerializeObject(obj, TestingJsonSettings.AllMembers) : "null";
}

public interface IAssertionContext<out T> : IAssertionContext
{
   T Actual { get; }
}

public abstract class AssertionContext : IAssertionContext
{
   public const string RemoveLine = nameof(RemoveLine);

   public string Expression { get; }

   protected AssertionContext(object? actual, string expression)
   {
      ActualUntyped = actual;
      Expression = NormalizeExpressionIndentation(expression);
   }

   public object? ActualUntyped { get; }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public IAssertionContext<T> Cast<T>() => new AssertionContext<T>((T)ActualUntyped, Expression);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

   public static readonly string Separator = "-".Repeat(50).Join();

   public string NormalizeExpressionIndentation(string expression)
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

public class AssertionContext<T> : AssertionContext, IAssertionContext<T>
{
   internal AssertionContext(T actual, string expression) : base(actual, expression) => Actual = actual;

   public T Actual { get; }
}
