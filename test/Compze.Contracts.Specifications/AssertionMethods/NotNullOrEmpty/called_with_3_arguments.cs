using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullOrEmpty;

public class called_with_3_arguments : NotNullOrEmptyTest
{
   [XF] public void does_not_throw_if_all_are_non_empty() =>
      Asserter.NotNullOrEmpty3("v", "v", "v").Must().Be(Asserter);

   [XF] public void argument_1_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty3(invalidValue, "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_2_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty3("v", invalidValue, "v"),
         "invalidValue");
   }

   [XF] public void argument_3_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty3("v", "v", invalidValue),
         "invalidValue");
   }
}