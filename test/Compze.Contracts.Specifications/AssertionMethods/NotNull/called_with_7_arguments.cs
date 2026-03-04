using Compze.Must;
using Compze.xUnit.BDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_7_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull7("a", "b", "c", "d", "e", "f", "g").Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_7_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;
      static readonly string? NullArg3 = null;
      static readonly string? NullArg4 = null;
      static readonly string? NullArg5 = null;
      static readonly string? NullArg6 = null;
      static readonly string? NullArg7 = null;

      [XF] public void argument_1_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7(NullArg1, "b", "c", "d", "e", "f", "g"), nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", NullArg2, "c", "d", "e", "f", "g"), nameof(NullArg2));

      [XF] public void argument_3_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", "b", NullArg3, "d", "e", "f", "g"), nameof(NullArg3));

      [XF] public void argument_4_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", "b", "c", NullArg4, "e", "f", "g"), nameof(NullArg4));

      [XF] public void argument_5_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", "b", "c", "d", NullArg5, "f", "g"), nameof(NullArg5));

      [XF] public void argument_6_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", "b", "c", "d", "e", NullArg6, "g"), nameof(NullArg6));

      [XF] public void argument_7_is_null() =>
         MustThrowContaining(() => Asserter.NotNull7("a", "b", "c", "d", "e", "f", NullArg7), nameof(NullArg7));
   }
}