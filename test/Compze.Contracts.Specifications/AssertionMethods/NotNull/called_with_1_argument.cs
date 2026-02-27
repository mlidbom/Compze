using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_1_argument : AssertionMethodsTest
{
   static readonly string? NullString = null;
   static readonly object? NullObject = null;

   [XF] public void does_not_throw_if_it_is_non_null() =>
      Asserter.NotNull("hello").Must().Be(Asserter);

   [XF] public void throws_if_it_is_null_string() =>
      Invoking(() => Asserter.NotNull(NullString)).Must().Throw<AssertionTestException>();

   [XF] public void throws_if_it_is_null_object() =>
      Invoking(() => Asserter.NotNull(NullObject)).Must().Throw<AssertionTestException>();

   [XF] public void exception_message_contains_the_argument_expression() =>
      Invoking(() => Asserter.NotNull(NullString))
         .Must().Throw<AssertionTestException>()
         .Which.Message.Must().Contain(nameof(NullString));
}
