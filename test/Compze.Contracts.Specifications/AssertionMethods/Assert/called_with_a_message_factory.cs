using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_a_message_factory : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_the_condition_is_true() =>
      Asserter.Assert(true, () => "should not appear").Must().Be(Asserter);

   [XF] public void throws_with_the_factory_produced_message_if_the_condition_is_false() =>
      Invoking(() => Asserter.Assert(false, () => "custom failure"))
         .Must().Throw<AssertionTestException>()
         .Which.Message.Must().Contain("custom failure");
}
