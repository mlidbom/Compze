using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;
// ReSharper disable PreferConcreteValueOverDefault

namespace Compze.Contracts.Specifications.AssertionMethods.NotDefault;

public class called_with_1_argument : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_it_is_non_default() =>
      Asserter.NotDefault(Guid.NewGuid()).Must().Be(Asserter);

   [XF] public void throws_with_message_containing_the_argument_expression()
   {
      Guid defaultGuid = default;
      MustThrowContaining(() => Asserter.NotDefault(defaultGuid), nameof(defaultGuid));
   }
}
