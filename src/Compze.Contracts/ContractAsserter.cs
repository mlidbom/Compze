using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Contracts;

public class ContractAsserter(Func<string, Exception> createException, Func<string, Exception> createNullException)
{
   readonly Func<string, Exception> _createException = createException;
   readonly Func<string, Exception> _createNullException = createNullException;

   [DoesNotReturn] public void ThrowFailed(string expression) => throw _createException(expression);

   [DoesNotReturn] public void ThrowNull(string expression) => throw _createNullException(expression);
}
