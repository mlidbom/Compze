using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Deprecated;
using Compze.Functional;

namespace Compze.Contracts;

class ContractAsserter(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;

   public ContractAsserter Is([DoesNotReturnIf(false)] bool value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ? this : throw _createException(valueString);

   public ContractAsserter NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value != null ? this : throw _createException(valueString);



   //////////////////////////////////////////////////////////////////////////
   //Specialized methods that return the value rather than `this` below here:
   //////////////////////////////////////////////////////////////////////////

   public ContractAsserter NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString))
                                 .then(this);

   [return: NotNull] public TValue ReturnNotNull<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      value ?? throw _createException(valueString);

   [return: NotNull] public TValue ReturnNotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
      NullOrDefaultTester<TValue>.AssertNotNullOrDefault(value, () => throw _createException(valueString));
}