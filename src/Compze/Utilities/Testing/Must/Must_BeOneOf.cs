using System.Linq;
using System.Runtime.CompilerServices;
// ReSharper disable ConvertClosureToMethodGroup

#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Fluent;

public static class Must_BeOneOf
{
   public static IMust<TValue> BeOneOf<TValue>(this IMust<TValue> must, TValue[] validValues, [CallerArgumentExpression(nameof(validValues))] string validValuesExpression = null!) =>
      must.SatisfyInternal(it => validValues.Contains(it),
                   usedArguments: [new(nameof(validValues), validValuesExpression, validValues)]);
}
