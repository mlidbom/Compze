using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>String null-or-empty assertion extensions for <see cref="ContractAsserter"/>.</summary>
public static class StringNotNullOrEmpty
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if the string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
      {
         if(string.IsNullOrEmpty(value)) @this.ThrowFailed(valueExpression);
         return @this;
      }

      ///<summary>Throws if either string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty2([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty3([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty4([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty5([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [NotNull] string? value5,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrEmpty(value5)) @this.ThrowFailed(expression5);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty6([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [NotNull] string? value5,
                                             [NotNull] string? value6,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                             [CallerArgumentExpression(nameof(value6))] string expression6 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrEmpty(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrEmpty(value6)) @this.ThrowFailed(expression6);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty7([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [NotNull] string? value5,
                                             [NotNull] string? value6,
                                             [NotNull] string? value7,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                             [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                             [CallerArgumentExpression(nameof(value7))] string expression7 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrEmpty(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrEmpty(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrEmpty(value7)) @this.ThrowFailed(expression7);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty8([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [NotNull] string? value5,
                                             [NotNull] string? value6,
                                             [NotNull] string? value7,
                                             [NotNull] string? value8,
                                             [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                             [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                             [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                             [CallerArgumentExpression(nameof(value4))] string expression4 = "",
                                             [CallerArgumentExpression(nameof(value5))] string expression5 = "",
                                             [CallerArgumentExpression(nameof(value6))] string expression6 = "",
                                             [CallerArgumentExpression(nameof(value7))] string expression7 = "",
                                             [CallerArgumentExpression(nameof(value8))] string expression8 = "")
      {
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrEmpty(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrEmpty(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrEmpty(value7)) @this.ThrowFailed(expression7);
         if(string.IsNullOrEmpty(value8)) @this.ThrowFailed(expression8);
         return @this;
      }

      ///<summary>Throws if any string is null or empty.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty9([NotNull] string? value1,
                                             [NotNull] string? value2,
                                             [NotNull] string? value3,
                                             [NotNull] string? value4,
                                             [NotNull] string? value5,
                                             [NotNull] string? value6,
                                             [NotNull] string? value7,
                                             [NotNull] string? value8,
                                             [NotNull] string? value9,
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
         if(string.IsNullOrEmpty(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrEmpty(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrEmpty(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrEmpty(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrEmpty(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrEmpty(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrEmpty(value7)) @this.ThrowFailed(expression7);
         if(string.IsNullOrEmpty(value8)) @this.ThrowFailed(expression8);
         if(string.IsNullOrEmpty(value9)) @this.ThrowFailed(expression9);
         return @this;
      }
   }
}
