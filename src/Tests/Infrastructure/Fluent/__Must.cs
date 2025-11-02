using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class __Must
{
   public static Must<T> Must<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string expression = null!) =>
      new(subject, expression);

   ///<summary>Allows for more correct sentences if one does not like just chaining the assertion methods themselves. Actually does nothing at all.</summary>
   public static Must<T> And<T>(this Must<T> @this) => @this;
}
