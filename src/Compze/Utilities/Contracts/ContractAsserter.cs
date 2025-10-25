using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Utilities.Contracts;

public partial class ContractAsserter(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;

   public ContractAsserter Is([DoesNotReturnIf(false)] bool value, Func<string>? createTessage = null,  [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw _createException( createTessage?.Invoke() ?? valueString);

   public ContractAsserter IsNotDisposed([DoesNotReturnIf(true)] bool isDisposed, object theInstance) =>
      !isDisposed ? this : throw new ObjectDisposedException(theInstance.GetType().FullName);
}
