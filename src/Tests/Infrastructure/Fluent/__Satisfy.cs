using System;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Satisfy
{
   public static IMust<T> Satisfy<T>(this IMust<T> context, Func<T, bool> predicate, Func<string>? messageOverride = null)
   {
      if(!predicate(context.Actual))
      {
         throw new AssertionFailedException(messageOverride?.Invoke() ?? context.Expression);
      }
      return context;
   }
}
