using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public partial class ContractAsserter
{
   public ContractAsserter NotNullOrEmpty([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      !string.IsNullOrEmpty(value) ? this : throw _createException($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");

   public ContractAsserter NotNullEmptyOrWhitespace([NotNull]string? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
   {
      NotNull(value, valueString);
      return !string.IsNullOrWhiteSpace(value) ? this : throw _createException($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
   }
}
