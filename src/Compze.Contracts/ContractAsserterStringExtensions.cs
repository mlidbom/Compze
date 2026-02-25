using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterStringExtensions
{
   extension(ContractAsserter @this)
   {
      public ContractAsserter NotNullOrEmpty([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      {
         if(string.IsNullOrEmpty(value)) @this.ThrowFailed($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
         return @this;
      }

      public ContractAsserter NotNullEmptyOrWhitespace([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      {
         @this.NotNull(value, valueString);
         if(string.IsNullOrWhiteSpace(value)) @this.ThrowFailed($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
         return @this;
      }
   }
}
