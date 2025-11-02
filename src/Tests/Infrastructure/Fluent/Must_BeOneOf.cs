using System.Linq;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0200
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must_BeOneOf
{
   public static Must<TValue> BeOneOf<TValue>(this Must<TValue> must, TValue[] validValues, [CallerArgumentExpression(nameof(validValues))] string validValuesExpression = null!) =>
      must.Satisfy(it => validValues.Contains(it),
                   usedArguments: [new(nameof(validValues), validValuesExpression, validValues)]);
}
