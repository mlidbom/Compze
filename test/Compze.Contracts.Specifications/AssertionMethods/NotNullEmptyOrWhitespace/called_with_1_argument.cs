using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullEmptyOrWhitespace;

public class called_with_1_argument : NotNullEmptyOrWhitespaceTest
{
   [XF] public void does_not_throw_if_it_is_non_whitespace() =>
      Asserter.NotNullEmptyOrWhitespace("hello").Must().Be(Asserter);

   [XF] public void throws_with_message_containing_the_argument_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace(invalidValue),
         "invalidValue");
   }
}