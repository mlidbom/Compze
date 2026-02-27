using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class IDisposableNotDisposed
{
   extension(ContractAsserter @this)
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDisposed([DoesNotReturnIf(true)] bool isDisposed, IDisposable theInstance)
      {
         ObjectDisposedException.ThrowIf(isDisposed, theInstance);
         return @this;
      }
   }
}
