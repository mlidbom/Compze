using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterIsExtensions
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter Is(this ContractAsserter asserter,
                                     [DoesNotReturnIf(false)] bool value,
                                     Func<string>? createMessage = null,
                                     [CallerArgumentExpression(nameof(value))] string valueString = "")
   {
      if(!value) asserter.ThrowFailed(createMessage?.Invoke() ?? valueString);
      return asserter;
   }

   public static ContractAsserter IsNotDisposed(this ContractAsserter asserter,
                                                [DoesNotReturnIf(true)] bool isDisposed,
                                                object theInstance)
   {
      if(isDisposed) throw new ObjectDisposedException(theInstance.GetType().FullName);
      return asserter;
   }
}
