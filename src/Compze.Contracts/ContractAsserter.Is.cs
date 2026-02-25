using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterIsExtensions
{
   extension(ContractAsserter @this)
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Is([DoesNotReturnIf(false)] bool value,
                                 [CallerArgumentExpression(nameof(value))] string valueString = "")
      {
         if(!value) @this.ThrowFailed(valueString);
         return @this;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDisposed([DoesNotReturnIf(true)] bool isDisposed, object theInstance)
      {
         ObjectDisposedException.ThrowIf(isDisposed, theInstance);
         return @this;
      }
   }
}
