using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterNotNullExtensions
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotNull<T>(this ContractAsserter asserter,
                                             [NotNull] T? value,
                                             [CallerArgumentExpression(nameof(value))] string expression = "")
      where T : class
   {
      if(value is null) asserter.ThrowNull(expression);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotNull2<T1, T2>(this ContractAsserter asserter,
                                                    [NotNull] T1? value1,
                                                    [NotNull] T2? value2,
                                                    [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                    [CallerArgumentExpression(nameof(value2))] string expression2 = "")
      where T1 : class
      where T2 : class
   {
      if(value1 is null) asserter.ThrowNull(expression1);
      if(value2 is null) asserter.ThrowNull(expression2);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotNull3<T1, T2, T3>(this ContractAsserter asserter,
                                                        [NotNull] T1? value1,
                                                        [NotNull] T2? value2,
                                                        [NotNull] T3? value3,
                                                        [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                        [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                        [CallerArgumentExpression(nameof(value3))] string expression3 = "")
      where T1 : class
      where T2 : class
      where T3 : class
   {
      if(value1 is null) asserter.ThrowNull(expression1);
      if(value2 is null) asserter.ThrowNull(expression2);
      if(value3 is null) asserter.ThrowNull(expression3);
      return asserter;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ContractAsserter NotNull4<T1, T2, T3, T4>(this ContractAsserter asserter,
                                                             [NotNull] T1? value1,
                                                             [NotNull] T2? value2,
                                                             [NotNull] T3? value3,
                                                             [NotNull] T4? value4,
                                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "")
      where T1 : class
      where T2 : class
      where T3 : class
      where T4 : class
   {
      if(value1 is null) asserter.ThrowNull(expression1);
      if(value2 is null) asserter.ThrowNull(expression2);
      if(value3 is null) asserter.ThrowNull(expression3);
      if(value4 is null) asserter.ThrowNull(expression4);
      return asserter;
   }
}
