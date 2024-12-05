using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Deprecated;
using Compze.Functional;

namespace Compze.Contracts;

partial class ContractAsserter
{
   public ContractAsserter NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value != null ? this : throw _createException(valueString);

   public ContractAsserter NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString))
                                 .then(this);

   public ContractAsserter NotDefault<TValue>(TValue value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      where TValue : struct, IEquatable<TValue>
      => !value.Equals(default) ? this : throw _createException(valueString);

   //////////////////////////////////////////////////////////////////////////
   ////Specialized methods that return the value rather than `this` below here:
   ////The allow for single line returns from methods while retaining the static not-null guarantee.
   //////////////////////////////////////////////////////////////////////////

   [return: NotNull] public TValue ReturnNotNull<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw _createException(valueString);

   [return: NotNull] public TValue ReturnNotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString));
}
