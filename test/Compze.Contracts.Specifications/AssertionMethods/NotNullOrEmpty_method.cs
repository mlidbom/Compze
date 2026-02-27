using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotNullOrEmpty_method : AssertionMethodsTest
{
   static readonly string? NullString = null;

   public class called_with_null_string : NotNullOrEmpty_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNullOrEmpty(NullString)).Must().Throw<AssertionTestException>();

      [XF] public void exception_message_contains_the_argument_expression() =>
         Invoking(() => Asserter.NotNullOrEmpty(NullString))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullString));
   }

   public class called_with_empty_string : NotNullOrEmpty_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNullOrEmpty("")).Must().Throw<AssertionTestException>();
   }

   public class called_with_non_empty_string : NotNullOrEmpty_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotNullOrEmpty("hello").Must().Be(Asserter);
   }
}
