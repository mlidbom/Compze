using Compze.Utilities.Functional;
using System;
using System.Linq;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public interface IMust<out T>
{
   string Separator { get; }
   string Expression { get; }
   T Actual { get; }
}

public class Must<T>(T subject, string expression) : IMust<T>
{
   // ReSharper disable once InconsistentNaming
   // ReSharper disable once StaticMemberInGenericType
   static readonly string _separator = "-".Repeat(50).Join();
   public string Separator => _separator;
   public T Actual { get; } = subject;
   public string Expression { get; } = NormalizeExpressionIndentation(expression);

   private static string NormalizeExpressionIndentation(string expression)
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
