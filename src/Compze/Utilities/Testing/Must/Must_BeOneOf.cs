using System.Linq;
using System.Runtime.CompilerServices;
// ReSharper disable ConvertClosureToMethodGroup

#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must_BeOneOf
{
   public static IAssertionContext<TValue> BeOneOf<TValue>(this IAssertionContext<TValue> assertionContext, TValue[] validValues, [CallerArgumentExpression(nameof(validValues))] string validValuesExpression = null!) =>
      assertionContext.SatisfyInternal(it => validValues.Contains(it),
                   usedArguments: [new(nameof(validValues), validValuesExpression, validValues)]);
}
