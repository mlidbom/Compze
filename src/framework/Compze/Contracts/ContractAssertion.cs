using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Deprecated;
using Compze.Functional;

namespace Compze.Contracts;

class ContractAssertion(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;

   public ContractAssertion Is([DoesNotReturnIf(false)] bool value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw _createException(valueString);

   public ContractAssertion NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value != null ? this : throw _createException(valueString);

   public ContractAssertion NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString))
                                 .then(this);

   [return: NotNull] public TValue ReturnNotNull<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw _createException(valueString);

   [return: NotNull] public TValue ReturnNotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString));
}

class StateAssertionException(string message) : InvalidOperationException(message);
class ArgumentAssertionException(string message) : ArgumentException(message);
class InvariantAssertionException(string message) : Exception(message);
class ResultAssertionException(string message) : Exception(message);
