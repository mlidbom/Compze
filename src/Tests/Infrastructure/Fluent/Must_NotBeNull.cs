
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must_NotBeNull
{
   public static IMust<TValue> NotBeNull<TValue>(this IMust<TValue?> must) =>
      must.Satisfy(it => it is not null).Cast<TValue>();

   public static IMust<TValue?> BeNull<TValue>(this IMust<TValue?> must) =>
      must.Satisfy(it => it is null);
}
