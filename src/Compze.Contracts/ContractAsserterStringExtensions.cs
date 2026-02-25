using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterStringExtensions
{
   extension(ContractAsserter @this)
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullOrEmpty([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
      {
         if(string.IsNullOrEmpty(value)) @this.ThrowFailed(valueExpression);
         return @this;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public ContractAsserter NotNullEmptyOrWhitespace([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
      {
         @this.NotNull(value, valueExpression);
         if(string.IsNullOrWhiteSpace(value)) @this.ThrowFailed(valueExpression);
         return @this;
      }
   }
}
