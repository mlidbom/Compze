using System;
using Compze.Utilities.Testing.Must;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Contracts.Specifications;

public abstract class AssertionMethodsTest
{
   internal static readonly ContractAsserter Asserter = new("Test", tessage => new AssertionTestException(tessage),
                                                                     tessage => new AssertionTestException(tessage));
   protected class AssertionTestException(string message) : Exception(message);

   protected static void MustThrowContaining(Action action, string expectedExpression) =>
      Invoking(action)
        .Must().Throw<AssertionTestException>()
        .Which.Message.Must().Contain(expectedExpression);

   protected static void MustThrowContainingForEach(string?[] invalidValues, Action<string?> action, string expectedExpression)
   {
      foreach(var invalidValue in invalidValues)
         MustThrowContaining(() => action(invalidValue), expectedExpression);
   }
}
