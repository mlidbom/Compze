using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_1_bool_argument : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_it_is_true() =>
      Asserter.Assert(true).Must().Be(Asserter);

   [XF] public void throws_if_it_is_false() =>
      Invoking(() => Asserter.Assert(false)).Must().Throw<AssertionTestException>();

   [XF] public void exception_message_contains_the_condition_expression()
   {
      const bool myCondition = false;
      Invoking(() => Asserter.Assert(myCondition))
         .Must().Throw<AssertionTestException>()
         .Which.Message.Must().Contain(nameof(myCondition));
   }
}
