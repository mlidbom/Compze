using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>Null-check assertion extensions for <see cref="ContractAsserter"/>.</summary>
// ReSharper disable once InconsistentNaming
public static class TNotNull
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if the value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull<T>([NotNull] T? value,
                                         [CallerArgumentExpression(nameof(value))] string expression = "")
      {
         if(value is null) @this.ThrowNull(expression);
         return @this;
      }

      ///<summary>Throws if either value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull2<T1, T2>([NotNull] T1? value1,
                                               [NotNull] T2? value2,
                                               [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                               [CallerArgumentExpression(nameof(value2))] string expression2 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull3<T1, T2, T3>([NotNull] T1? value1,
                                                   [NotNull] T2? value2,
                                                   [NotNull] T3? value3,
                                                   [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                   [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                   [CallerArgumentExpression(nameof(value3))] string expression3 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull4<T1, T2, T3, T4>([NotNull] T1? value1,
                                                       [NotNull] T2? value2,
                                                       [NotNull] T3? value3,
                                                       [NotNull] T4? value4,
                                                       [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                       [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                       [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                       [CallerArgumentExpression(nameof(value4))] string expression4 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull5<T1, T2, T3, T4, T5>([NotNull] T1? value1,
                                                           [NotNull] T2? value2,
                                                           [NotNull] T3? value3,
                                                           [NotNull] T4? value4,
                                                           [NotNull] T5? value5,
                                                           [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                           [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                           [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                           [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                           [CallerArgumentExpression(nameof(value5))] string expression5 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         if(value5 is null) @this.ThrowNull(expression5);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull6<T1, T2, T3, T4, T5, T6>([NotNull] T1? value1,
                                                               [NotNull] T2? value2,
                                                               [NotNull] T3? value3,
                                                               [NotNull] T4? value4,
                                                               [NotNull] T5? value5,
                                                               [NotNull] T6? value6,
                                                               [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                               [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                               [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                               [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                               [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                               [CallerArgumentExpression(nameof(value6))] string expression6 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         if(value5 is null) @this.ThrowNull(expression5);
         if(value6 is null) @this.ThrowNull(expression6);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull7<T1, T2, T3, T4, T5, T6, T7>([NotNull] T1? value1,
                                                                   [NotNull] T2? value2,
                                                                   [NotNull] T3? value3,
                                                                   [NotNull] T4? value4,
                                                                   [NotNull] T5? value5,
                                                                   [NotNull] T6? value6,
                                                                   [NotNull] T7? value7,
                                                                   [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                   [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                   [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                   [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                   [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                   [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                   [CallerArgumentExpression(nameof(value7))] string expression7 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         if(value5 is null) @this.ThrowNull(expression5);
         if(value6 is null) @this.ThrowNull(expression6);
         if(value7 is null) @this.ThrowNull(expression7);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull8<T1, T2, T3, T4, T5, T6, T7, T8>([NotNull] T1? value1,
                                                                       [NotNull] T2? value2,
                                                                       [NotNull] T3? value3,
                                                                       [NotNull] T4? value4,
                                                                       [NotNull] T5? value5,
                                                                       [NotNull] T6? value6,
                                                                       [NotNull] T7? value7,
                                                                       [NotNull] T8? value8,
                                                                       [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                       [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                       [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                       [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                       [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                       [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                       [CallerArgumentExpression(nameof(value7))] string expression7 = "",
                                                                       [CallerArgumentExpression(nameof(value8))] string expression8 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         if(value5 is null) @this.ThrowNull(expression5);
         if(value6 is null) @this.ThrowNull(expression6);
         if(value7 is null) @this.ThrowNull(expression7);
         if(value8 is null) @this.ThrowNull(expression8);
         return @this;
      }

      ///<summary>Throws if any value is null.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull9<T1, T2, T3, T4, T5, T6, T7, T8, T9>([NotNull] T1? value1,
                                                                           [NotNull] T2? value2,
                                                                           [NotNull] T3? value3,
                                                                           [NotNull] T4? value4,
                                                                           [NotNull] T5? value5,
                                                                           [NotNull] T6? value6,
                                                                           [NotNull] T7? value7,
                                                                           [NotNull] T8? value8,
                                                                           [NotNull] T9? value9,
                                                                           [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                           [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                           [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                           [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                           [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                           [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                           [CallerArgumentExpression(nameof(value7))] string expression7 = "",
                                                                           [CallerArgumentExpression(nameof(value8))] string expression8 = "",
                                                                           [CallerArgumentExpression(nameof(value9))] string expression9 = "")
      {
         if(value1 is null) @this.ThrowNull(expression1);
         if(value2 is null) @this.ThrowNull(expression2);
         if(value3 is null) @this.ThrowNull(expression3);
         if(value4 is null) @this.ThrowNull(expression4);
         if(value5 is null) @this.ThrowNull(expression5);
         if(value6 is null) @this.ThrowNull(expression6);
         if(value7 is null) @this.ThrowNull(expression7);
         if(value8 is null) @this.ThrowNull(expression8);
         if(value9 is null) @this.ThrowNull(expression9);
         return @this;
      }
   }
}
