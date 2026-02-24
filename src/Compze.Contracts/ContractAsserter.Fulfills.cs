using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterFulfillsExtensions
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter Fulfills(this ContractAsserter asserter,
                                           [DoesNotReturnIf(false)] bool assert1,
                                           [CallerArgumentExpression(nameof(assert1))] string expression1 = "")
   {
      if(!assert1) asserter.ThrowFailed(expression1);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter Fulfills(this ContractAsserter asserter,
                                           [DoesNotReturnIf(false)] bool assert1,
                                           [DoesNotReturnIf(false)] bool assert2,
                                           [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                           [CallerArgumentExpression(nameof(assert2))] string expression2 = "")
   {
      if(!assert1) asserter.ThrowFailed(expression1);
      if(!assert2) asserter.ThrowFailed(expression2);
      return asserter;
   }
}
