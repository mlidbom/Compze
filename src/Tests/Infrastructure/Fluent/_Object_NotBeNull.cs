using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectNullCheckAssertions
{
   public static Must<TValue> NotBeNull<TValue>(this Must<TValue> must) =>
      must.Satisfy(it => it is not null);

   public static Must<TValue?> BeNull<TValue>(this Must<TValue?> must) =>
      must.Satisfy(it => it is null);
}
