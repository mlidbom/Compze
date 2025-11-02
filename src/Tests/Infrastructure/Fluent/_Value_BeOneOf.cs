using System.Linq;

#pragma warning disable IDE0200

namespace Compze.Tests.Infrastructure.Fluent;

public static class ValueAssertions
{
   public static Must<TValue> BeOneOf<TValue>(this Must<TValue> must, params TValue[] validValues) =>
      must.Satisfy(it => validValues.Contains(it),
                   usedArguments: [new(nameof(validValues), "validValues", validValues)]);
}
