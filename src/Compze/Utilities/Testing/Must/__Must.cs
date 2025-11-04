using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class __Must
{
   public static IMust<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new Must<T>(subject, expression);

   ///<summary>Allows for more correct sentences if one does not like just chaining the assertion methods themselves. Actually does nothing at all.</summary>
   public static IMust<T> And<T>(this IMust<T> @this) => @this;
}
