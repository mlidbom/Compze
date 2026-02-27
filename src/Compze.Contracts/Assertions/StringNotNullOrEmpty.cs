using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>String null-or-empty assertion extension for <see cref="ContractAsserter"/>.</summary>
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
   }
}
