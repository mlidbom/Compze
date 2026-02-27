using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotDefault;

public class called_with_3_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_default() =>
      Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Must().Be(Asserter);

   public class throws_if : called_with_3_arguments
   {
      [XF] public void argument_1_is_default() =>
         Invoking(() => Asserter.NotDefault(default(Guid), Guid.NewGuid(), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), default(Guid), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), default(Guid))).Must().Throw<AssertionTestException>();
   }
}
