using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Contracts;

public class ContractAsserter(Func<string, Exception> createException, Func<string, Exception> createNullException)
{
   readonly Func<string, Exception> _createException = createException;
   readonly Func<string, Exception> _createNullException = createNullException;

   /// <summary>Throws the asserter's configured exception with the given expression. Use this from custom assertion extension methods.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   [DoesNotReturn] public void ThrowFailed(string expression) => throw _createException(expression);

   /// <summary>Throws the asserter's configured null exception with the given expression. Use this from custom assertion extension methods.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   [DoesNotReturn] public void ThrowNull(string expression) => throw _createNullException(expression);
}
