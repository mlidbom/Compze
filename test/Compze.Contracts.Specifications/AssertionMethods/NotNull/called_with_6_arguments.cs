using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_6_arguments : AssertionMethodsTest
{
   static readonly string? NullString = null;

   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull6("a", "b", "c", "d", "e", "f").Must().Be(Asserter);

   public class throws_if : called_with_6_arguments
   {
      [XF] public void argument_1_is_null() =>
         Invoking(() => Asserter.NotNull6(NullString, "b", "c", "d", "e", "f")).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_null() =>
         Invoking(() => Asserter.NotNull6("a", NullString, "c", "d", "e", "f")).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", NullString, "d", "e", "f")).Must().Throw<AssertionTestException>();

      [XF] public void argument_4_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", NullString, "e", "f")).Must().Throw<AssertionTestException>();

      [XF] public void argument_5_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", "d", NullString, "f")).Must().Throw<AssertionTestException>();

      [XF] public void argument_6_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", "d", "e", NullString)).Must().Throw<AssertionTestException>();
   }

   public class exception_message_contains_the_argument_expression_if : called_with_6_arguments
   {
      static readonly string? NullArg1 = null;
      static readonly string? NullArg2 = null;
      static readonly string? NullArg3 = null;
      static readonly string? NullArg4 = null;
      static readonly string? NullArg5 = null;
      static readonly string? NullArg6 = null;

      [XF] public void argument_1_is_null() =>
         Invoking(() => Asserter.NotNull6(NullArg1, "b", "c", "d", "e", "f"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg1));

      [XF] public void argument_2_is_null() =>
         Invoking(() => Asserter.NotNull6("a", NullArg2, "c", "d", "e", "f"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg2));

      [XF] public void argument_3_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", NullArg3, "d", "e", "f"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg3));

      [XF] public void argument_4_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", NullArg4, "e", "f"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg4));

      [XF] public void argument_5_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", "d", NullArg5, "f"))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg5));

      [XF] public void argument_6_is_null() =>
         Invoking(() => Asserter.NotNull6("a", "b", "c", "d", "e", NullArg6))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullArg6));
   }
}
