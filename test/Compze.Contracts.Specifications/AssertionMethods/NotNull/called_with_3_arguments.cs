using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_3_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull3("a", "b", "c").Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_3_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;
      static readonly string? NullArg3 = null;

      [XF] public void argument_1_is_null() =>
         MustThrowContaining(() => Asserter.NotNull3(NullArg1, "b", "c"), nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         MustThrowContaining(() => Asserter.NotNull3("a", NullArg2, "c"), nameof(NullArg2));

      [XF] public void argument_3_is_null() =>
         MustThrowContaining(() => Asserter.NotNull3("a", "b", NullArg3), nameof(NullArg3));
   }
}