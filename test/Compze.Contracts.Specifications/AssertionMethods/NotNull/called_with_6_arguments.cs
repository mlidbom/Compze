using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_6_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull6("a", "b", "c", "d", "e", "f").Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_6_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;
      static readonly string? NullArg3 = null;
      static readonly string? NullArg4 = null;
      static readonly string? NullArg5 = null;
      static readonly string? NullArg6 = null;

      [XF] public void argument_1_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6(NullArg1, "b", "c", "d", "e", "f"), nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6("a", NullArg2, "c", "d", "e", "f"), nameof(NullArg2));

      [XF] public void argument_3_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6("a", "b", NullArg3, "d", "e", "f"), nameof(NullArg3));

      [XF] public void argument_4_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6("a", "b", "c", NullArg4, "e", "f"), nameof(NullArg4));

      [XF] public void argument_5_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6("a", "b", "c", "d", NullArg5, "f"), nameof(NullArg5));

      [XF] public void argument_6_is_null() =>
         MustThrowContaining(() => Asserter.NotNull6("a", "b", "c", "d", "e", NullArg6), nameof(NullArg6));
   }
}