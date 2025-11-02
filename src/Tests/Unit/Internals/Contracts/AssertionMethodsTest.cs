using System;
using System.Runtime.CompilerServices;
using Compze.Utilities.Contracts;
using Compze.Tests.Infrastructure.Fluent;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

namespace Compze.Tests.Unit.Internals.Contracts;

public abstract class AssertionMethodsTest
{
   internal static readonly ContractAsserter Asserter = new(tessage => new AssertionTestException(tessage));
   protected class AssertionTestException(string message) : Exception(message);

   // ReSharper disable once EntityNameCapturedOnly.Global : Yes. Capturing its name is the entire point of passing it :)
   internal static void ThrowsAndCapturesArgumentExpressionText(Func<ContractAsserter> assertFunc, object? value, [CallerArgumentExpression(nameof(value))] string valueExpressionString = "")
   {
      Invoking(() => assertFunc())
                     .Must().Throw<AssertionTestException>()
                   .Which.Message
                   .Must().Contain(valueExpressionString);
   }
}
