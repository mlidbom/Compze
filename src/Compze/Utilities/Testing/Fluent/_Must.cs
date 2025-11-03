using System;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;

#pragma warning disable CA1033 // The compiler is unhappy about the explicit interface implementation below

namespace Compze.Utilities.Testing.Fluent;

public interface IMust
{
   string Expression { get; }
   object? ActualUntyped { get; }
   string Separator { get; }
   IMust<T> Cast<T>();
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
       {Separator}
       Failing assertion:
       {Separator}
       {AssertionCode(method, predicate)}
       {Separator}
       """;
}

public interface IMust<out T> : IMust
{
   T Actual { get; }
}

public abstract class Must : IMust
{
   public string Expression { get; }

   protected Must(object? actual, string expression)
   {
      ActualUntyped = actual;
      Expression = NormalizeExpressionIndentation(expression);
   }

   public object? ActualUntyped { get; }

   string IMust.Separator => Separator;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public IMust<T> Cast<T>() => new Must<T>((T)ActualUntyped, Expression);
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

public class Must<T> : Must, IMust<T>
{
   public Must(T actual, string expression) : base(actual, expression) => Actual = actual;

   public new string Separator => Must.Separator;

   string IMust.Separator => Separator;
   public T Actual { get; }
}
