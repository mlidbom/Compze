using System.Runtime.CompilerServices;

// ReSharper disable ConvertClosureToMethodGroup

#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must_BeOneOf
{
   public static IAssertionContext<TValue> BeOneOf<TValue>(this IAssertionContext<TValue> context, TValue[] validValues, [CallerArgumentExpression(nameof(validValues))] string validValuesExpression = null!) =>
      context.SatisfyInternal(it => validValues.Contains(it),
                              expressionValues: [new(validValuesExpression, validValues)]);

   ///<summary>Throws if the enum value is not one of the declared values of the enum type</summary>
   public static IAssertionContext<TEnum> BeValidEnumValue<TEnum>(this IAssertionContext<TEnum> context)
      where TEnum : struct, Enum =>
      context.SatisfyInternal(it => it.IsValid());
}
