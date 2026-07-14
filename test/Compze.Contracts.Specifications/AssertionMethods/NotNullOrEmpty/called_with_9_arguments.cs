using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullOrEmpty;

public class called_with_9_arguments : NotNullOrEmptyTest
{
   [XF] public void does_not_throw_if_all_are_non_empty() =>
      Asserter.NotNullOrEmpty9("v", "v", "v", "v", "v", "v", "v", "v", "v").Must().Be(Asserter);

   [XF] public void argument_1_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9(invalidValue, "v", "v", "v", "v", "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_2_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", invalidValue, "v", "v", "v", "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_3_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", invalidValue, "v", "v", "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_4_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", invalidValue, "v", "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_5_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", "v", invalidValue, "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_6_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", "v", "v", invalidValue, "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_7_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", "v", "v", "v", invalidValue, "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_8_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", "v", "v", "v", "v", invalidValue, "v"),
         "invalidValue");
   }

   [XF] public void argument_9_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullOrEmpty9("v", "v", "v", "v", "v", "v", "v", "v", invalidValue),
         "invalidValue");
   }
}
