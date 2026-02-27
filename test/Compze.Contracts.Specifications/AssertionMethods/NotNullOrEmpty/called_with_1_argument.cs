using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullOrEmpty;

public class called_with_1_argument : AssertionMethodsTest
{
   static readonly string? NullString = null;

   [XF] public void does_not_throw_if_it_is_non_empty() =>
      Asserter.NotNullOrEmpty("hello").Must().Be(Asserter);

   [XF] public void exception_message_contains_the_argument_expression() =>
      Invoking(() => Asserter.NotNullOrEmpty(NullString))
         .Must().Throw<AssertionTestException>()
         .Which.Message.Must().Contain(nameof(NullString));

   public class throws_if : called_with_1_argument
   {
      [XF] public void it_is_null() =>
         Invoking(() => Asserter.NotNullOrEmpty(NullString)).Must().Throw<AssertionTestException>();

      [XF] public void it_is_empty() =>
         Invoking(() => Asserter.NotNullOrEmpty("")).Must().Throw<AssertionTestException>();
   }
}
