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

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [DoesNotReturnIf(false)] bool assert5,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "",
                                     [CallerArgumentExpression(nameof(assert5))] string expression5 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         if(!assert5) @this.ThrowFailed(expression5);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [DoesNotReturnIf(false)] bool assert5,
                                     [DoesNotReturnIf(false)] bool assert6,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "",
                                     [CallerArgumentExpression(nameof(assert5))] string expression5 = "",
                                     [CallerArgumentExpression(nameof(assert6))] string expression6 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         if(!assert5) @this.ThrowFailed(expression5);
         if(!assert6) @this.ThrowFailed(expression6);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [DoesNotReturnIf(false)] bool assert5,
                                     [DoesNotReturnIf(false)] bool assert6,
                                     [DoesNotReturnIf(false)] bool assert7,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "",
                                     [CallerArgumentExpression(nameof(assert5))] string expression5 = "",
                                     [CallerArgumentExpression(nameof(assert6))] string expression6 = "",
                                     [CallerArgumentExpression(nameof(assert7))] string expression7 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         if(!assert5) @this.ThrowFailed(expression5);
         if(!assert6) @this.ThrowFailed(expression6);
         if(!assert7) @this.ThrowFailed(expression7);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [DoesNotReturnIf(false)] bool assert5,
                                     [DoesNotReturnIf(false)] bool assert6,
                                     [DoesNotReturnIf(false)] bool assert7,
                                     [DoesNotReturnIf(false)] bool assert8,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "",
                                     [CallerArgumentExpression(nameof(assert5))] string expression5 = "",
                                     [CallerArgumentExpression(nameof(assert6))] string expression6 = "",
                                     [CallerArgumentExpression(nameof(assert7))] string expression7 = "",
                                     [CallerArgumentExpression(nameof(assert8))] string expression8 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         if(!assert5) @this.ThrowFailed(expression5);
         if(!assert6) @this.ThrowFailed(expression6);
         if(!assert7) @this.ThrowFailed(expression7);
         if(!assert8) @this.ThrowFailed(expression8);
         return @this;
      }

      ///<summary>Throws if any condition is false.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter Assert([DoesNotReturnIf(false)] bool assert1,
                                     [DoesNotReturnIf(false)] bool assert2,
                                     [DoesNotReturnIf(false)] bool assert3,
                                     [DoesNotReturnIf(false)] bool assert4,
                                     [DoesNotReturnIf(false)] bool assert5,
                                     [DoesNotReturnIf(false)] bool assert6,
                                     [DoesNotReturnIf(false)] bool assert7,
                                     [DoesNotReturnIf(false)] bool assert8,
                                     [DoesNotReturnIf(false)] bool assert9,
                                     [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                     [CallerArgumentExpression(nameof(assert2))] string expression2 = "",
                                     [CallerArgumentExpression(nameof(assert3))] string expression3 = "",
                                     [CallerArgumentExpression(nameof(assert4))] string expression4 = "",
                                     [CallerArgumentExpression(nameof(assert5))] string expression5 = "",
                                     [CallerArgumentExpression(nameof(assert6))] string expression6 = "",
                                     [CallerArgumentExpression(nameof(assert7))] string expression7 = "",
                                     [CallerArgumentExpression(nameof(assert8))] string expression8 = "",
                                     [CallerArgumentExpression(nameof(assert9))] string expression9 = "")
      {
         if(!assert1) @this.ThrowFailed(expression1);
         if(!assert2) @this.ThrowFailed(expression2);
         if(!assert3) @this.ThrowFailed(expression3);
         if(!assert4) @this.ThrowFailed(expression4);
         if(!assert5) @this.ThrowFailed(expression5);
         if(!assert6) @this.ThrowFailed(expression6);
         if(!assert7) @this.ThrowFailed(expression7);
         if(!assert8) @this.ThrowFailed(expression8);
         if(!assert9) @this.ThrowFailed(expression9);
         return @this;
      }
   }
}
