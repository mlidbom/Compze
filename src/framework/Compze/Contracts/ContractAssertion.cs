using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Deprecated;

namespace Compze.Contracts;

abstract class ContractAssertion
{
   protected abstract Exception CreateException(string message);

   public ContractAssertion Is([DoesNotReturnIf(false)] bool value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw CreateException(valueString);

   public TValue NotNull<TValue>(TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw CreateException(valueString);

   public TValue NotNullOrDefault<TValue>(TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      => !NullOrDefaultTester<TValue>.IsNullOrDefault(value) ? value! : throw CreateException(valueString);
}

class ArgumentAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new ArgumentException(message);
}

class InvariantAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new InvariantException(message);
}

class StateAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new InvalidOperationException(message);
}

class ResultAssertion : ContractAssertion
{
   protected override Exception CreateException(string message) => new AssertionException(message);
}

class InvariantException(string message) : Exception(message);
class AssertionException(string message) : Exception(message);
