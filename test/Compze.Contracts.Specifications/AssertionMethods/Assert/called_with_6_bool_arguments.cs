using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_6_bool_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_true() =>
      Asserter.Assert(true, true, true, true, true, true).Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_6_bool_arguments
   {
      const bool Condition1 = false;
      const bool Condition2 = false;
      const bool Condition3 = false;
      const bool Condition4 = false;
      const bool Condition5 = false;
      const bool Condition6 = false;

      [XF] public void argument_1_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(Condition1, true, true, true, true, true), nameof(Condition1));

      [XF] public void argument_2_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, Condition2, true, true, true, true), nameof(Condition2));

      [XF] public void argument_3_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, true, Condition3, true, true, true), nameof(Condition3));

      [XF] public void argument_4_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, true, true, Condition4, true, true), nameof(Condition4));

      [XF] public void argument_5_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, true, true, true, Condition5, true), nameof(Condition5));

      [XF] public void argument_6_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, true, true, true, true, Condition6), nameof(Condition6));
   }
}