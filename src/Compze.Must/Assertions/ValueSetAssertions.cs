using System.Runtime.CompilerServices;

// ReSharper disable ConvertClosureToMethodGroup

#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions that a value is among a set of allowed values.</summary>
public static class ValueSetAssertions
{
   /// <summary>Asserts that the value equals one of <paramref name="validValues"/>.</summary>
   public static IAssertionContext<TValue> BeOneOf<TValue>(this IAssertionContext<TValue> context, TValue[] validValues, [CallerArgumentExpression(nameof(validValues))] string validValuesExpression = null!) =>
      context.RunAssertion(it => validValues.Contains(it),
                           expressionValues: [new(validValuesExpression, validValues)]);

   ///<summary>Throws if the enum value is not one of the declared values of the enum type</summary>
   public static IAssertionContext<TEnum> BeValidEnumValue<TEnum>(this IAssertionContext<TEnum> context)
      where TEnum : struct, Enum =>
      context.RunAssertion(it => it.IsValid());
}
