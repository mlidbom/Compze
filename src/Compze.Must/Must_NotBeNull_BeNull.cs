
// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Null assertions.</summary>
public static class Must_NotBeNull
{
   /// <summary>Asserts that the value is not <see langword="null"/>, narrowing the context to the non-nullable type.</summary>
   public static IAssertionContext<TValue> NotBeNull<TValue>(this IAssertionContext<TValue?> context) =>
      context.SatisfyInternal(it => it is not null).Cast<TValue>();

   /// <summary>Asserts that the value is <see langword="null"/>.</summary>
   public static IAssertionContext<TValue?> BeNull<TValue>(this IAssertionContext<TValue?> context) =>
      context.SatisfyInternal(it => it is null);
}
