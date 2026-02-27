using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_7_bool_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_true() =>
      Asserter.Assert(true, true, true, true, true, true, true).Must().Be(Asserter);

   public class throws_if : called_with_7_bool_arguments
   {
      [XF] public void argument_1_is_false() =>
         Invoking(() => Asserter.Assert(false, true, true, true, true, true, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_false() =>
         Invoking(() => Asserter.Assert(true, false, true, true, true, true, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_false() =>
         Invoking(() => Asserter.Assert(true, true, false, true, true, true, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_4_is_false() =>
         Invoking(() => Asserter.Assert(true, true, true, false, true, true, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_5_is_false() =>
         Invoking(() => Asserter.Assert(true, true, true, true, false, true, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_6_is_false() =>
         Invoking(() => Asserter.Assert(true, true, true, true, true, false, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_7_is_false() =>
         Invoking(() => Asserter.Assert(true, true, true, true, true, true, false)).Must().Throw<AssertionTestException>();
   }

   public class exception_message_contains_the_argument_expression_if : called_with_7_bool_arguments
   {
      const bool Condition1 = false;
      const bool Condition2 = false;
      const bool Condition3 = false;
      const bool Condition4 = false;
      const bool Condition5 = false;
      const bool Condition6 = false;
      const bool Condition7 = false;

      [XF] public void argument_1_is_invalid() =>
         Invoking(() => Asserter.Assert(Condition1, true, true, true, true, true, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition1));

      [XF] public void argument_2_is_invalid() =>
         Invoking(() => Asserter.Assert(true, Condition2, true, true, true, true, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition2));

      [XF] public void argument_3_is_invalid() =>
         Invoking(() => Asserter.Assert(true, true, Condition3, true, true, true, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition3));

      [XF] public void argument_4_is_invalid() =>
         Invoking(() => Asserter.Assert(true, true, true, Condition4, true, true, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition4));

      [XF] public void argument_5_is_invalid() =>
         Invoking(() => Asserter.Assert(true, true, true, true, Condition5, true, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition5));

      [XF] public void argument_6_is_invalid() =>
         Invoking(() => Asserter.Assert(true, true, true, true, true, Condition6, true))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition6));

      [XF] public void argument_7_is_invalid() =>
         Invoking(() => Asserter.Assert(true, true, true, true, true, true, Condition7))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(Condition7));
   }
}
