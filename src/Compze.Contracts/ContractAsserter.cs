using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Exceptions;
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBeInternal

namespace Compze.Contracts;

/// <summary>Fluent assertion builder. Assertion methods are added via extension methods and return <c>this</c> for chaining.</summary>
public class ContractAsserter(string name, Func<string, Exception> createException, Func<string, Exception> createNullException)
{
   readonly string _name = name;
   readonly Func<string, Exception> _createException = createException;
   readonly Func<string, Exception> _createNullException = createNullException;

   /// <summary>Throws the asserter's configured exception. Message format: <c>{name}.{CallerMemberName}({expression})</c>. Use this from custom assertion extension methods.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   [DoesNotReturn] public void ThrowFailed(string expression, [CallerMemberName] string? callerName = null) => throw _createException($"{_name}.{callerName}({expression})");

   /// <summary>
   /// Throws the asserter's configured exception with <paramref name="message"/> as its whole message: for an assertion whose caller wrote the<br/>
   /// failure message, which, unlike an expression, reads wrong wrapped in the <see cref="ThrowFailed"/> format.
   /// </summary>
   [DoesNotReturn] internal void ThrowFailedWithMessage(string message) => throw _createException(message);

   /// <summary>Throws the asserter's configured null exception with the raw expression as the parameter name. Use this from custom assertion extension methods.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   [DoesNotReturn] public void ThrowNull(string expression) => throw _createNullException(expression);


   ///<summary>Assert conditions about current state of "this". Failures throw <see cref="InvalidOperationException"/>.</summary>
   public ContractAsserter State => Contract.State;

   ///<summary>Assert something that must always be true for "this". Failures throw <see cref="InvariantAssertionFailedException"/></summary>
   public ContractAsserter Invariant => Contract.Invariant;

   ///<summary>Assert conditions on arguments to the current method. Failures throw <see cref="ArgumentException"/></summary>
   public ContractAsserter Argument => Contract.Argument;
}
