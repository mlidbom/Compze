using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_3_arguments : AssertionMethodsTest
{
   static readonly string? NullString = null;

   [XF] public void does_not_throw_if_all_are_non_null() =>
      Asserter.NotNull3("a", "b", "c").Must().Be(Asserter);

   public class throws_if : called_with_3_arguments
   {
      [XF] public void argument_1_is_null() =>
         Invoking(() => Asserter.NotNull3(NullString, "b", "c")).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_null() =>
         Invoking(() => Asserter.NotNull3("a", NullString, "c")).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_null() =>
         Invoking(() => Asserter.NotNull3("a", "b", NullString)).Must().Throw<AssertionTestException>();
   }
}
