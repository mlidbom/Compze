using Compze.Must;
using Compze.xUnit.BDD;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullEmptyOrWhitespace;

public class called_with_6_arguments : NotNullEmptyOrWhitespaceTest
{
   [XF] public void does_not_throw_if_all_are_non_whitespace() =>
      Asserter.NotNullEmptyOrWhitespace6("v", "v", "v", "v", "v", "v").Must().Be(Asserter);

   [XF] public void argument_1_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6(invalidValue, "v", "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_2_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6("v", invalidValue, "v", "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_3_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6("v", "v", invalidValue, "v", "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_4_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6("v", "v", "v", invalidValue, "v", "v"),
         "invalidValue");
   }

   [XF] public void argument_5_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6("v", "v", "v", "v", invalidValue, "v"),
         "invalidValue");
   }

   [XF] public void argument_6_throws_with_expression_for_each_invalid_value()
   {
      MustThrowContainingForEach(InvalidValues,
         invalidValue => Asserter.NotNullEmptyOrWhitespace6("v", "v", "v", "v", "v", invalidValue),
         "invalidValue");
   }
}