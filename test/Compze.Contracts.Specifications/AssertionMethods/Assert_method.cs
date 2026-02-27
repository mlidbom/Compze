using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class Assert_method : AssertionMethodsTest
{
   public class called_with_1_bool_argument : Assert_method
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

   public class called_with_2_bool_arguments : Assert_method
   {
      [XF] public void does_not_throw_if_all_are_true() =>
         Asserter.Assert(true, true).Must().Be(Asserter);

      public class throws_if : called_with_2_bool_arguments
      {
         [XF] public void argument_1_is_false() =>
            Invoking(() => Asserter.Assert(false, true)).Must().Throw<AssertionTestException>();

         [XF] public void argument_2_is_false() =>
            Invoking(() => Asserter.Assert(true, false)).Must().Throw<AssertionTestException>();
      }
   }

   public class called_with_a_message_factory : Assert_method
   {
      [XF] public void does_not_throw_if_the_condition_is_true() =>
         Asserter.Assert(true, () => "should not appear").Must().Be(Asserter);

      [XF] public void throws_with_the_factory_produced_message_if_the_condition_is_false() =>
         Invoking(() => Asserter.Assert(false, () => "custom failure"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain("custom failure");
   }
}
