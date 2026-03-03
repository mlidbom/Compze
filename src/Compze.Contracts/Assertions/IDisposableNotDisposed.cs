using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>Disposed-state assertion extension for <see cref="ContractAsserter"/>.</summary>
public static class IDisposableNotDisposed
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [DoesNotReturn] static void ThrowObjectDisposed(IDisposable theInstance) => throw new ObjectDisposedException(theInstance.GetType().FullName);

   extension(ContractAsserter @this)
   {
      ///<summary>Throws <see cref="ObjectDisposedException"/> if the instance has been disposed.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDisposed([DoesNotReturnIf(true)] bool isDisposed, IDisposable theInstance)
      {
         if(isDisposed) ThrowObjectDisposed(theInstance);
         return @this;
      }
   }
}
