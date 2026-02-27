using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_2_bool_arguments : AssertionMethodsTest
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

   public class exception_message_contains_the_argument_expression_if : called_with_2_bool_arguments
   {
      const bool Condition1 = false;
      const bool Condition2 = false;

      [XF] public void argument_1_is_invalid() =>
         Invoking(() => Asserter.Assert(Condition1, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition1));

      [XF] public void argument_2_is_invalid() =>
         Invoking(() => Asserter.Assert(true, Condition2))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition2));
   }
}
