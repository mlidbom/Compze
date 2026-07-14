using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullOrEmpty;

public class called_with_2_arguments : NotNullOrEmptyTest
{
   [XF] public void does_not_throw_if_all_are_non_empty() =>
      Asserter.NotNullOrEmpty2("v", "v").Must().Be(Asserter);

   [XF] public void argument_1_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty2(invalidValue, "v"),
         "invalidValue");
   }

   [XF] public void argument_2_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty2("v", invalidValue),
         "invalidValue");
   }
}
