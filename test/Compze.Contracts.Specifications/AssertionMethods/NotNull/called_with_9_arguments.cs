using Compze.Must;
using Compze.xUnit.BDD;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_9_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull9("a", "b", "c", "d", "e", "f", "g", "h", "i").Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_9_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;
      static readonly string? NullArg3 = null;
      static readonly string? NullArg4 = null;
      static readonly string? NullArg5 = null;
      static readonly string? NullArg6 = null;
      static readonly string? NullArg7 = null;
      static readonly string? NullArg8 = null;
      static readonly string? NullArg9 = null;

      [XF] public void argument_1_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9(NullArg1, "b", "c", "d", "e", "f", "g", "h", "i"), nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", NullArg2, "c", "d", "e", "f", "g", "h", "i"), nameof(NullArg2));

      [XF] public void argument_3_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", NullArg3, "d", "e", "f", "g", "h", "i"), nameof(NullArg3));

      [XF] public void argument_4_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", NullArg4, "e", "f", "g", "h", "i"), nameof(NullArg4));

      [XF] public void argument_5_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", "d", NullArg5, "f", "g", "h", "i"), nameof(NullArg5));

      [XF] public void argument_6_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", "d", "e", NullArg6, "g", "h", "i"), nameof(NullArg6));

      [XF] public void argument_7_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", "d", "e", "f", NullArg7, "h", "i"), nameof(NullArg7));

      [XF] public void argument_8_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", "d", "e", "f", "g", NullArg8, "i"), nameof(NullArg8));

      [XF] public void argument_9_is_null() =>
         MustThrowContaining(() => Asserter.NotNull9("a", "b", "c", "d", "e", "f", "g", "h", NullArg9), nameof(NullArg9));
   }
}