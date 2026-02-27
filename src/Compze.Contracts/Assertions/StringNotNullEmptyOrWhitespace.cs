using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable CS8777 // On netstandard2.0 the BCL string methods lack nullable annotations, but our checks guarantee non-null at exit

namespace Compze.Contracts;

/// <summary>String null/empty/whitespace assertion extensions for <see cref="ContractAsserter"/>.</summary>
public static class StringNotNullEmptyOrWhitespace
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if the string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
      {
         if(string.IsNullOrWhiteSpace(value)) @this.ThrowFailed(valueExpression);
         return @this;
      }

      ///<summary>Throws if either string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace2([NotNull] string? value1,
                                                       [NotNull] string? value2,
                                                       [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                       [CallerArgumentExpression(nameof(value2))] string expression2 = "")
      {
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace3([NotNull] string? value1,
                                                       [NotNull] string? value2,
                                                       [NotNull] string? value3,
                                                       [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                       [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                       [CallerArgumentExpression(nameof(value3))] string expression3 = "")
      {
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace4([NotNull] string? value1,
                                                       [NotNull] string? value2,
                                                       [NotNull] string? value3,
                                                       [NotNull] string? value4,
                                                       [CallerArgumentExpression(nameof(value1))] string expression1 = "",
                                                       [CallerArgumentExpression(nameof(value2))] string expression2 = "",
                                                       [CallerArgumentExpression(nameof(value3))] string expression3 = "",
                                                       [CallerArgumentExpression(nameof(value4))] string expression4 = "")
      {
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace5([NotNull] string? value1,
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
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrWhiteSpace(value5)) @this.ThrowFailed(expression5);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace6([NotNull] string? value1,
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
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrWhiteSpace(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrWhiteSpace(value6)) @this.ThrowFailed(expression6);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace7([NotNull] string? value1,
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
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrWhiteSpace(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrWhiteSpace(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrWhiteSpace(value7)) @this.ThrowFailed(expression7);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace8([NotNull] string? value1,
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
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrWhiteSpace(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrWhiteSpace(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrWhiteSpace(value7)) @this.ThrowFailed(expression7);
         if(string.IsNullOrWhiteSpace(value8)) @this.ThrowFailed(expression8);
         return @this;
      }

      ///<summary>Throws if any string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace9([NotNull] string? value1,
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
         if(string.IsNullOrWhiteSpace(value1)) @this.ThrowFailed(expression1);
         if(string.IsNullOrWhiteSpace(value2)) @this.ThrowFailed(expression2);
         if(string.IsNullOrWhiteSpace(value3)) @this.ThrowFailed(expression3);
         if(string.IsNullOrWhiteSpace(value4)) @this.ThrowFailed(expression4);
         if(string.IsNullOrWhiteSpace(value5)) @this.ThrowFailed(expression5);
         if(string.IsNullOrWhiteSpace(value6)) @this.ThrowFailed(expression6);
         if(string.IsNullOrWhiteSpace(value7)) @this.ThrowFailed(expression7);
         if(string.IsNullOrWhiteSpace(value8)) @this.ThrowFailed(expression8);
         if(string.IsNullOrWhiteSpace(value9)) @this.ThrowFailed(expression9);
         return @this;
      }
   }
}
