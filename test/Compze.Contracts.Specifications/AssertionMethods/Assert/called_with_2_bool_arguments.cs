using Compze.Must;

using Compze.xUnitBDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_2_bool_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_true() =>
      Asserter.Assert(true, true).Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_2_bool_arguments
   {
      const bool Condition1 = false;
      const bool Condition2 = false;

      [XF] public void argument_1_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(Condition1, true), nameof(Condition1));

      [XF] public void argument_2_is_invalid() =>
         MustThrowContaining(() => Asserter.Assert(true, Condition2), nameof(Condition2));
   }
}
