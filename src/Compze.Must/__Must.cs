using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Must;

public static class __Must
{
   public static IAssertionContext<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new AssertionContext<T>(subject, expression);
}
