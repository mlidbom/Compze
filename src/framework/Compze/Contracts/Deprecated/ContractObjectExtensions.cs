using System;

namespace Compze.Contracts.Deprecated;

static class ContractObjectExtensions
{
   public static T Assert<T>(this T @this, bool assert, string message = "")
   {
      Contract.Assert.That(assert, message);
      return @this;
   }
}