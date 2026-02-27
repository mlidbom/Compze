using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_2_bool_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_true() =>
      Asserter.Assert(true, true).Must().Be(Asserter);

   public class throws_if : called_with_2_bool_arguments
   {
      [XF] public void argument_1_is_false() =>
         Invoking(() => Asserter.Assert(false, true)).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_false() =>
         Invoking(() => Asserter.Assert(true, false)).Must().Throw<AssertionTestException>();
   }
}
