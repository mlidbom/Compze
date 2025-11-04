
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must_NotBeNull
{
   public static IAssertionContext<TValue> NotBeNull<TValue>(this IAssertionContext<TValue?> assertionContext) =>
      assertionContext.SatisfyInternal(it => it is not null).Cast<TValue>();

   public static IAssertionContext<TValue?> BeNull<TValue>(this IAssertionContext<TValue?> assertionContext) =>
      assertionContext.SatisfyInternal(it => it is null);
}
