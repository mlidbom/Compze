using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullOrEmpty;

public class called_with_1_argument : NotNullOrEmptyTest
{
   [XF] public void does_not_throw_if_it_is_non_empty() =>
      Asserter.NotNullOrEmpty("hello").Must().Be(Asserter);

   [XF] public void throws_with_message_containing_the_argument_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty(invalidValue),
         "invalidValue");
   }
}