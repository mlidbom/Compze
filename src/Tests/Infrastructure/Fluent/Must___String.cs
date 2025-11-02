using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must___String
{
   public static Must<string> Contain(this Must<string> must, string expected) =>
      must.Satisfy(it => it.ContainsOrdinal(expected), messageOverride: _ => BuildMessage("did not contain the expected string:", must, expected));

   public static Must<string> NotContain(this Must<string> must, string unexpected) =>
      must.Satisfy(it => !it.ContainsOrdinal(unexpected), messageOverride: _ => BuildMessage("contained the unexpected string:", must, unexpected));

   public static Must<string>? StartWith(this Must<string> must, string expected) =>
      must.Satisfy(it => it.StartsWithOrdinal(expected), messageOverride: _ => BuildMessage("did not start with the expected string:", must, expected));

   public static Must<string>? EndWith(this Must<string> must, string expected) =>
      must.Satisfy(it => it.EndsWithOrdinal(expected), messageOverride: _ => BuildMessage("did not end with the expected string:", must, expected));

   static string BuildMessage(string message, Must<string> must, string expected)
   {
      return $"""""
              {must.Separator}
              the string produced by the expression: 
              {must.Separator}
              {must.Expression.Indent()}
              {must.Separator}
              {message}
              {must.Separator}
              {expected}
              {must.Separator}
              instead it was:
              {must.Separator}
              {must.Actual}
              {must.Separator}
              """"";
   }
}
