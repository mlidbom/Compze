// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Null assertions.</summary>
public static class NullabilityAssertions
{
   /// <summary>Asserts that the value is not <see langword="null"/>, narrowing the context to the non-nullable type.</summary>
   public static IAssertionContext<TValue> NotBeNull<TValue>(this IAssertionContext<TValue?> context) =>
      context.RunAssertion(it => it is not null).Cast<TValue>();

   /// <summary>Asserts that the nullable value type holds a value, narrowing the context to the underlying value type: an <c>int?</c> becomes an <c>int</c>.</summary>
   /// <remarks>Every assertion over a value type, such as <see cref="BooleanAssertions.BeTrue"/>, is reached through this from the nullable form of that type.</remarks>
   public static IAssertionContext<TValue> NotBeNull<TValue>(this IAssertionContext<TValue?> context) where TValue : struct =>
      context.RunAssertion(it => it.HasValue).Cast<TValue>();

   /// <summary>Asserts that the value is <see langword="null"/>.</summary>
   public static IAssertionContext<TValue?> BeNull<TValue>(this IAssertionContext<TValue?> context) =>
      context.RunAssertion(it => it is null);
}
