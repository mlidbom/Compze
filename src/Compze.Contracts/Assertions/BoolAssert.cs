using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>Boolean assertion extensions for <see cref="ContractAsserter"/>.</summary>
public static class BoolAssert
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if <paramref name="value"/> is false, using <paramref name="createMessage"/> to produce the failure message.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool value,
                                       Func<string> createMessage)
      {
         if(!value) @this.ThrowFailed(createMessage.Invoke());
         return @this;
      }

      ///<summary>Throws if the condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                       [CallerArgumentExpression(nameof(assert1))] string expression1 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         return @this;
      }

      ///<summary>Throws if either condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
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
