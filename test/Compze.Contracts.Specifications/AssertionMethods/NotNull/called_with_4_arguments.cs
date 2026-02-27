using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_4_arguments : AssertionMethodsTest
{
   static readonly string? NullString = null;

   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull4("a", "b", "c", "d").Must().Be(Asserter);

   public class throws_if : called_with_4_arguments
   {
      [XF] public void argument_1_is_null() =>
         Invoking(() => Asserter.NotNull4(NullString, "b", "c", "d")).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_null() =>
         Invoking(() => Asserter.NotNull4("a", NullString, "c", "d")).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_null() =>
         Invoking(() => Asserter.NotNull4("a", "b", NullString, "d")).Must().Throw<AssertionTestException>();

      [XF] public void argument_4_is_null() =>
         Invoking(() => Asserter.NotNull4("a", "b", "c", NullString)).Must().Throw<AssertionTestException>();
   }
}
