using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterNullOrEmptyExtensions
{
   public static ContractAsserter NotNullOrDefault<TValue>(this ContractAsserter asserter, [NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") where TValue : struct
   {
      if(value == null || Equals(value, default(TValue))) asserter.ThrowFailed(valueString);
      return asserter;
   }

   //////////////////////////////////////////////////////////////////////////
   ////Specialized methods that return the value rather than `this` below here:
   ////The allow for single line returns from methods while retaining the static not-null guarantee.
   //////////////////////////////////////////////////////////////////////////

   [return: NotNull] public static TValue ReturnNotNull<TValue>(this ContractAsserter asserter, [NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw new AssertionFailedException(valueString);

   public static TValue ReturnNotNullOrDefault<TValue>(this ContractAsserter asserter, [NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") where TValue : struct
   {
      asserter.NotNullOrDefault(value, valueString);
      return (TValue)value;
   }

   public static TValue ReturnNotDefault<TValue>(this ContractAsserter asserter, TValue value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      where TValue : struct
      => !value.Equals(default(TValue)) ? value : throw new AssertionFailedException(valueString);
}
