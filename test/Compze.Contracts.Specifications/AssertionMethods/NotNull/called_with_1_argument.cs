using Compze.Must;
using Compze.xUnit.BDD;
using static Compze.Must.MustActions;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNull;

public class called_with_1_argument : AssertionMethodsTest
{
   static readonly object? NullObject = null;

   [XF] public void does_not_throw_if_it_is_non_null() =>
      Asserter.NotNull("hello").Must().Be(Asserter);

   [XF] public void throws_if_it_is_null_object() =>
      Invoking(() => Asserter.NotNull(NullObject)).Must().Throw<AssertionTestException>();

   [XF] public void throws_with_message_containing_the_argument_expression()
   {
      string? nullString = null;
      MustThrowContaining(() => Asserter.NotNull(nullString), nameof(nullString));
   }
}
