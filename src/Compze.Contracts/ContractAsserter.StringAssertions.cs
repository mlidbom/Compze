using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterStringExtensions
{
   public static ContractAsserter NotNullOrEmpty(this ContractAsserter asserter, [NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
   {
      if(string.IsNullOrEmpty(value)) asserter.ThrowFailed($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
      return asserter;
   }

   public static ContractAsserter NotNullEmptyOrWhitespace(this ContractAsserter asserter, [NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
   {
      asserter.NotNull(value, valueString);
      if(string.IsNullOrWhiteSpace(value)) asserter.ThrowFailed($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
      return asserter;
   }
}
