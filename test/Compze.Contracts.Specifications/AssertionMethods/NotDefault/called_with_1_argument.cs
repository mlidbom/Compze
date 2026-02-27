using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Contracts.Specifications.AssertionMethods.NotDefault;

public class called_with_1_argument : AssertionMethodsTest
{
   static readonly Guid DefaultGuid = default;

   [XF] public void does_not_throw_if_it_is_non_default() =>
      Asserter.NotDefault(Guid.NewGuid()).Must().Be(Asserter);

   [XF] public void throws_if_it_is_default() =>
      Invoking(() => Asserter.NotDefault(DefaultGuid)).Must().Throw<AssertionTestException>();

   [XF] public void exception_message_contains_the_argument_expression() =>
      Invoking(() => Asserter.NotDefault(DefaultGuid))
         .Must().Throw<AssertionTestException>()
         .Which.Message.Must().Contain(nameof(DefaultGuid));
}
