using System;
using System.Runtime.CompilerServices;
using Compze.Contracts;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Contracts;

public abstract class AssertionMethodsTest
{
   internal static readonly ContractAsserter Asserter = new(message => new AssertionTestException(message));
   protected class AssertionTestException(string message) : Exception(message);

   // ReSharper disable once EntityNameCapturedOnly.Global : Yes. Capturing its name is the entire point of passing it :)
   internal static void ThrowsAndCapturesArgumentExpressionText(Func<ContractAsserter> assertFunc, object value, [CallerArgumentExpression(nameof(value))] string valueExpressionString = "")
   {
      FluentActions.Invoking(assertFunc)
                   .Should().Throw<AssertionTestException>()
                   .Which.Message
                   .Should().Contain(valueExpressionString);
   }
}
