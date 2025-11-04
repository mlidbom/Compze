using System;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;

#pragma warning disable CA1033 // The compiler is unhappy about the explicit interface implementation below

namespace Compze.Utilities.Testing.Must;

public interface IAssertionContext
{
   string Expression { get; }
   IAssertionContext<T> Cast<T>();
   string NormalizeExpressionIndentation(string expression);

   string AssertionCode(string method, string? predicate = null, AssertionArgumentInfo[]? arguments = null)
   {
      var argumentsText = arguments != null && arguments.Any()
                         ? arguments.Select(it => it.Expression).Join(", ")
                         : "";

      return $"{Expression}.Must().{method}({predicate})";

   }

   string FailingAssertionHeading(string method, string? predicate = null, AssertionArgumentInfo[]? arguments = null) =>
      $"""
       {AssertionContext.Separator}
       Failing assertion:
       {AssertionContext.Separator}
       {AssertionCode(method, predicate)}
       {AssertionContext.Separator}
       """;

   string AssertionMethodCall(string callerName, string? predicate = null, AssertionArgumentInfo[]? usedArguments = null)
   {
      if(string.IsNullOrEmpty(callerName))
         return AssertionContext.RemoveLine;

      var arguments = usedArguments != null && usedArguments.Any()
                         ? usedArguments.Select(it => it.Expression).Join(", ")
                         : "";

      return $"""
              {Expression}.Must().{callerName}({arguments})
              {AssertionContext.Separator}
              """;
   }
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

   // ReSharper disable once MemberCanBeMadeStatic.Global
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
