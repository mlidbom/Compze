using Compze.Must;
using Compze.xUnit.BDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_2_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull2("a", "b").Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_2_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;

      [XF] public void argument_1_is_null() =>
         MustThrowContaining(() => Asserter.NotNull2(NullArg1, "valid"), nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         MustThrowContaining(() => Asserter.NotNull2("valid", NullArg2), nameof(NullArg2));
   }
}