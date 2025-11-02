using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must___String
{
   public static IMust<string> Contain(this IMust<string> must, string expected) =>
      must.Satisfy(it => it.ContainsOrdinal(expected), messageOverride: _ => BuildMessage("did not contain the expected string:", must, expected));

   public static IMust<string> NotContain(this IMust<string> must, string unexpected) =>
      must.Satisfy(it => !it.ContainsOrdinal(unexpected), messageOverride: _ => BuildMessage("contained the unexpected string:", must, unexpected));

   public static IMust<string>? StartWith(this IMust<string> must, string expected) =>
      must.Satisfy(it => it.StartsWithOrdinal(expected), messageOverride: _ => BuildMessage("did not start with the expected string:", must, expected));

   public static IMust<string>? EndWith(this IMust<string> must, string expected) =>
      must.Satisfy(it => it.EndsWithOrdinal(expected), messageOverride: _ => BuildMessage("did not end with the expected string:", must, expected));

   static string BuildMessage(string message, IMust<string> must, string expected)
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
