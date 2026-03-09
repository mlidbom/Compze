using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>Default-value assertion extensions for <see cref="ContractAsserter"/>.</summary>
// ReSharper disable once InconsistentNaming
public static class TStructNotDefault
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if the value equals <c>default(T)</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T>(T value,
                                            [CallerArgumentExpression(nameof(value))] string expression = "")
         where T : struct
      {
         if(value.Equals(default(T))) @this.ThrowFailed(expression);
         return @this;
      }

      ///<summary>Throws if either value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2>(T1 value1,
                                                 T2 value2,
                                                 [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                 [CallerArgumentExpression(nameof(value2))] string expression2 = "")
         where T1 : struct
         where T2 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3>(T1 value1,
                                                     T2 value2,
                                                     T3 value3,
                                                     [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                     [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                     [CallerArgumentExpression(nameof(value3))] string expression3 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4>(T1 value1,
                                                         T2 value2,
                                                         T3 value3,
                                                         T4 value4,
                                                         [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                         [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                         [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                         [CallerArgumentExpression(nameof(value4))] string expression4 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4, T5>(T1 value1,
                                                             T2 value2,
                                                             T3 value3,
                                                             T4 value4,
                                                             T5 value5,
                                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
         where T5 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         if(value5.Equals(default(T5))) @this.ThrowFailed(expression5);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4, T5, T6>(T1 value1,
                                                                 T2 value2,
                                                                 T3 value3,
                                                                 T4 value4,
                                                                 T5 value5,
                                                                 T6 value6,
                                                                 [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                 [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                 [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                 [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                 [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                 [CallerArgumentExpression(nameof(value6))] string expression6 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
         where T5 : struct
         where T6 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         if(value5.Equals(default(T5))) @this.ThrowFailed(expression5);
         if(value6.Equals(default(T6))) @this.ThrowFailed(expression6);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4, T5, T6, T7>(T1 value1,
                                                                     T2 value2,
                                                                     T3 value3,
                                                                     T4 value4,
                                                                     T5 value5,
                                                                     T6 value6,
                                                                     T7 value7,
                                                                     [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                     [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                     [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                     [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                     [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                     [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                     [CallerArgumentExpression(nameof(value7))] string expression7 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
         where T5 : struct
         where T6 : struct
         where T7 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         if(value5.Equals(default(T5))) @this.ThrowFailed(expression5);
         if(value6.Equals(default(T6))) @this.ThrowFailed(expression6);
         if(value7.Equals(default(T7))) @this.ThrowFailed(expression7);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1,
                                                                         T2 value2,
                                                                         T3 value3,
                                                                         T4 value4,
                                                                         T5 value5,
                                                                         T6 value6,
                                                                         T7 value7,
                                                                         T8 value8,
                                                                         [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                         [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                         [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                         [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                         [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                         [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                         [CallerArgumentExpression(nameof(value7))] string expression7 = "",
                                                                         [CallerArgumentExpression(nameof(value8))] string expression8 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
         where T5 : struct
         where T6 : struct
         where T7 : struct
         where T8 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         if(value5.Equals(default(T5))) @this.ThrowFailed(expression5);
         if(value6.Equals(default(T6))) @this.ThrowFailed(expression6);
         if(value7.Equals(default(T7))) @this.ThrowFailed(expression7);
         if(value8.Equals(default(T8))) @this.ThrowFailed(expression8);
         return @this;
      }

      ///<summary>Throws if any value equals its <c>default</c>.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotDefault<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 value1,
                                                                             T2 value2,
                                                                             T3 value3,
                                                                             T4 value4,
                                                                             T5 value5,
                                                                             T6 value6,
                                                                             T7 value7,
                                                                             T8 value8,
                                                                             T9 value9,
                                                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                                                             [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                                                             [CallerArgumentExpression(nameof(value7))] string expression7 = "",
                                                                             [CallerArgumentExpression(nameof(value8))] string expression8 = "",
                                                                             [CallerArgumentExpression(nameof(value9))] string expression9 = "")
         where T1 : struct
         where T2 : struct
         where T3 : struct
         where T4 : struct
         where T5 : struct
         where T6 : struct
         where T7 : struct
         where T8 : struct
         where T9 : struct
      {
         if(value1.Equals(default(T1))) @this.ThrowFailed(expression1);
         if(value2.Equals(default(T2))) @this.ThrowFailed(expression2);
         if(value3.Equals(default(T3))) @this.ThrowFailed(expression3);
         if(value4.Equals(default(T4))) @this.ThrowFailed(expression4);
         if(value5.Equals(default(T5))) @this.ThrowFailed(expression5);
         if(value6.Equals(default(T6))) @this.ThrowFailed(expression6);
         if(value7.Equals(default(T7))) @this.ThrowFailed(expression7);
         if(value8.Equals(default(T8))) @this.ThrowFailed(expression8);
         if(value9.Equals(default(T9))) @this.ThrowFailed(expression9);
         return @this;
      }
   }
}
