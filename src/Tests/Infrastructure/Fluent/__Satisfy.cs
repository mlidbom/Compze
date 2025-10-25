using System;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Satisfy
{
   public static IAssertionBuilder<T> Satisfy<T>(this IAssertionBuilder<T> builder, Func<T, bool> predicate, string tessage)
   {
      if(!predicate(builder.Subject))
      {
         throw new AssertionFailedException(tessage);
      }
      return builder;
   }
}
