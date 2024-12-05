using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Deprecated;
using Compze.Functional;

// ReSharper disable MemberCanBeMadeStatic.Global : Statics and fluent interfaces do not mix.

namespace Compze.Contracts;

abstract class ContractAssertion
{
   protected abstract Exception CreateException(string message);

   public ContractAssertion Is([DoesNotReturnIf(false)] bool value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw CreateException(valueString);

   public ContractAssertion NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value != null ? this : throw CreateException(valueString);

   public ContractAssertion NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw CreateException(valueString))
                                 .then(this);
}

class ArgumentAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new ArgumentAssertionException(message);
}

class InvariantAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new InvariantAssertionException(message);
}

class StateAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new InvalidOperationException(message);
}

class ResultAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new ResultAssertionException(message);
}

class ReturnAssertion
{
   static Exception CreateException(string message) => new ResultAssertionException(message);

   [return: NotNull] public TValue NotNull<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw CreateException(valueString);

   [return: NotNull] public TValue NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw CreateException(valueString));
}

class StateAssertionException(string message) : InvalidOperationException(message);
class ArgumentAssertionException(string message) : ArgumentException(message);
class InvariantAssertionException(string message) : Exception(message);
class ResultAssertionException(string message) : Exception(message);
