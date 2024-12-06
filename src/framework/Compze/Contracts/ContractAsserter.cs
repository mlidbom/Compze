using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

partial class ContractAsserter(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;

   public ContractAsserter Is([DoesNotReturnIf(false)] bool value, Func<string>? createMessage = null,  [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw _createException( createMessage?.Invoke() ?? valueString);

   public ContractAsserter IsNotDisposed([DoesNotReturnIf(true)] bool isDisposed, object theInstance) =>
      !isDisposed ? this : throw new ObjectDisposedException(theInstance.GetType().FullName);
}
