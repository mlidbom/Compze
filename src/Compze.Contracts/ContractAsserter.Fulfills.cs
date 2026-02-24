using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterFulfillsExtensions
{
   extension(ContractAsserter @this)
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Fulfills([DoesNotReturnIf(false)] bool assert1,
                                       [CallerArgumentExpression(nameof(assert1))] string expression1 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         return @this;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Fulfills([DoesNotReturnIf(false)] bool assert1,
                                       [DoesNotReturnIf(false)] bool assert2,
                                       [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                       [CallerArgumentExpression(nameof(assert2))] string expression2 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         return @this;
      }
   }
}
