using Compze.Utilities.Functional;
using System;
using System.Linq;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

class Must
{
   public static readonly string Separator = "-".Repeat(50).Join();
}

public class Must<T>
{
   public Must(T subject, string expression)
   {
      Actual = subject;
      Expression = NormalizeExpressionIndentation(expression);
   }

   // ReSharper disable once InconsistentNaming
   // ReSharper disable once StaticMemberInGenericType
   public string Separator => Must.Separator;
   public T Actual { get; }
   public string Expression { get; }

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
