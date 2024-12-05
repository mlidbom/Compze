using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

partial class ContractAsserter(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;

   public ContractAsserter Is([DoesNotReturnIf(false)] bool value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw _createException(valueString);
}
