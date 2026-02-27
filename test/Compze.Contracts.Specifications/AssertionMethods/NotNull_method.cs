using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotNull_method : AssertionMethodsTest
{
   static readonly string? NullString = null;
   static readonly object? NullObject = null;

   public class called_with_null_string : NotNull_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNull(NullString)).Must().Throw<AssertionTestException>();

      [XF] public void exception_message_contains_the_argument_expression() =>
         Invoking(() => Asserter.NotNull(NullString))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(NullString));
   }

   public class called_with_null_object : NotNull_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNull(NullObject)).Must().Throw<AssertionTestException>();
   }

   public class called_with_non_null_value : NotNull_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotNull("hello").Must().Be(Asserter);
   }

   public class NotNull2_called_with_first_value_null : NotNull_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNull2(NullString, "valid")).Must().Throw<AssertionTestException>();
   }

   public class NotNull2_called_with_second_value_null : NotNull_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotNull2("valid", NullString)).Must().Throw<AssertionTestException>();
   }

   public class NotNull2_called_with_both_non_null : NotNull_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotNull2("a", "b").Must().Be(Asserter);
   }
}
