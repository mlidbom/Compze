using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>String null/empty/whitespace assertion extension for <see cref="ContractAsserter"/>.</summary>
public static class StringNotNullEmptyOrWhitespace
{
   extension(ContractAsserter @this)
   {
      ///<summary>Throws if the string is null, empty, or contains only whitespace.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
      {
         @this.NotNull(value, valueExpression);
         if(string.IsNullOrWhiteSpace(value)) @this.ThrowFailed(valueExpression);
         return @this;
      }
   }
}
