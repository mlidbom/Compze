using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class Assert_method : AssertionMethodsTest
{
   public class called_with_a_false_condition : Assert_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.Assert(false)).Must().Throw<AssertionTestException>();

      [XF] public void exception_message_contains_the_condition_expression()
      {
         const bool myCondition = false;
         Invoking(() => Asserter.Assert(myCondition))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(myCondition));
      }
   }

   public class called_with_a_true_condition : Assert_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.Assert(true).Must().Be(Asserter);
   }

   public class called_with_two_conditions_where_the_first_is_false : Assert_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.Assert(false, true)).Must().Throw<AssertionTestException>();
   }

   public class called_with_two_conditions_where_the_second_is_false : Assert_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.Assert(true, false)).Must().Throw<AssertionTestException>();
   }

   public class called_with_two_true_conditions : Assert_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.Assert(true, true).Must().Be(Asserter);
   }

   public class called_with_a_message_factory_and_a_false_condition : Assert_method
   {
      [XF] public void throws_with_the_factory_produced_message() =>
         Invoking(() => Asserter.Assert(false, () => "custom failure"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain("custom failure");
   }

   public class called_with_a_message_factory_and_a_true_condition : Assert_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.Assert(true, () => "should not appear").Must().Be(Asserter);
   }
}
