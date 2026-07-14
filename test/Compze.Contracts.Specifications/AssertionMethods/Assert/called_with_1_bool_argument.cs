using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.Assert;

public class called_with_1_bool_argument : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_it_is_true() =>
      Asserter.Assert(true).Must().Be(Asserter);

   [XF] public void throws_with_message_containing_the_condition_expression()
   {
      const bool myCondition = false;
      MustThrowContaining(() => Asserter.Assert(myCondition), nameof(myCondition));
   }
}
