using Compze.Utilities.Functional;
using System;
using System.Linq;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class Must
{
   public string Expression { get; }

   public Must(object? actual, string expression)
   {
      ActualUntyped = actual;
      Expression = NormalizeExpressionIndentation(expression);
   }

   public object? ActualUntyped { get; }


#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public Must<T> Cast<T>() => new((T)ActualUntyped, Expression);
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

public class Must<T> : Must
{
   public Must(T actual, string expression) : base(actual, expression) => Actual = actual;


   public new string Separator => Must.Separator;
   public T Actual { get; }
}
