using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class __Must
{
   public static IAssertionContext<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new AssertionContext<T>(subject, expression);

   ///<summary>Allows for more correct sentences if one does not like just chaining the assertion methods themselves. Actually does nothing at all.</summary>
   public static IAssertionContext<T> And<T>(this IAssertionContext<T> @this) => @this;
}
