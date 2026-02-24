using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterNotDefaultExtensions
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotDefault<T>(this ContractAsserter asserter,
                                                T value,
                                                [CallerArgumentExpression(nameof(value))] string expression = "")
      where T : struct
   {
      if(value.Equals(default(T))) asserter.ThrowFailed(expression);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotDefault2<T1, T2>(this ContractAsserter asserter,
                                                      T1 value1,
                                                      T2 value2,
                                                      [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                      [CallerArgumentExpression(nameof(value2))] string expression2 = "")
      where T1 : struct
      where T2 : struct
   {
      if(value1.Equals(default(T1))) asserter.ThrowFailed(expression1);
      if(value2.Equals(default(T2))) asserter.ThrowFailed(expression2);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotDefault3<T1, T2, T3>(this ContractAsserter asserter,
                                                           T1 value1,
                                                           T2 value2,
                                                           T3 value3,
                                                           [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                           [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                           [CallerArgumentExpression(nameof(value3))] string expression3 = "")
      where T1 : struct
      where T2 : struct
      where T3 : struct
   {
      if(value1.Equals(default(T1))) asserter.ThrowFailed(expression1);
      if(value2.Equals(default(T2))) asserter.ThrowFailed(expression2);
      if(value3.Equals(default(T3))) asserter.ThrowFailed(expression3);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotDefault4<T1, T2, T3, T4>(this ContractAsserter asserter,
                                                               T1 value1,
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
      if(value1.Equals(default(T1))) asserter.ThrowFailed(expression1);
      if(value2.Equals(default(T2))) asserter.ThrowFailed(expression2);
      if(value3.Equals(default(T3))) asserter.ThrowFailed(expression3);
      if(value4.Equals(default(T4))) asserter.ThrowFailed(expression4);
      return asserter;
   }
}
