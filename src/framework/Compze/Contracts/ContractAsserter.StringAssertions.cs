using System.Runtime.CompilerServices;
using Compze.SystemCE;

namespace Compze.Contracts;

partial class ContractAsserter
{
   public ContractAsserter NotNullOrEmpty(string value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      !string.IsNullOrEmpty(value) ? this : throw _createException($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");

   public ContractAsserter NotNullEmptyOrWhitespace(string value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      !value.IsNullEmptyOrWhiteSpace() ? this : throw _createException($"{valueString} was '{value}' which is {nameof(NotNullEmptyOrWhitespace)}");
}
