using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class __Must
{
   public static IMust<T> Must<T>(this T subject,
                                  [CallerArgumentExpression(nameof(subject))]
                                  string expression = null!) => new Must<T>(subject, expression);
}
