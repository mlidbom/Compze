using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class AnyNotNull
{
   extension(ContractAsserter @this)
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNull<T>([NotNull] T? value,
                                         [CallerArgumentExpression(nameof(value))] string expression = "")
      {
         if(value is null) @this.ThrowNull(expression);
         return @this;
      }

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
   }
}
