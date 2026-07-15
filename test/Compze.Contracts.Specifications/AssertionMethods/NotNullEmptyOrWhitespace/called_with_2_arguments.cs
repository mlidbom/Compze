using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullEmptyOrWhitespace;

public class called_with_2_arguments : NotNullEmptyOrWhitespaceTest
{
   [XF] public void does_not_throw_if_all_are_non_whitespace() =>
      Asserter.NotNullEmptyOrWhitespace2("v", "v").Must().Be(Asserter);

   [XF] public void argument_1_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace2(invalidValue, "v"),
         "invalidValue");
   }

   [XF] public void argument_2_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace2("v", invalidValue),
         "invalidValue");
   }
}
