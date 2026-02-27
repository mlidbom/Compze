using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotNull_method : AssertionMethodsTest
{
   static readonly string? NullString = null;
   static readonly object? NullObject = null;

   public class called_with_1_argument : NotNull_method
   {
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

   public class called_with_2_arguments : NotNull_method
   {
      [XF] public void does_not_throw_if_all_are_non_null() =>
         Asserter.NotNull2("a", "b").Must().Be(Asserter);

      public class throws_if : called_with_2_arguments
      {
         [XF] public void argument_1_is_null() =>
            Invoking(() => Asserter.NotNull2(NullString, "valid")).Must().Throw<AssertionTestException>();

         [XF] public void argument_2_is_null() =>
            Invoking(() => Asserter.NotNull2("valid", NullString)).Must().Throw<AssertionTestException>();
      }
   }

   public class called_with_3_arguments : NotNull_method
   {
      [XF] public void does_not_throw_if_all_are_non_null() =>
         Asserter.NotNull3("a", "b", "c").Must().Be(Asserter);

      public class throws_if : called_with_3_arguments
      {
         [XF] public void argument_1_is_null() =>
            Invoking(() => Asserter.NotNull3(NullString, "b", "c")).Must().Throw<AssertionTestException>();

         [XF] public void argument_2_is_null() =>
            Invoking(() => Asserter.NotNull3("a", NullString, "c")).Must().Throw<AssertionTestException>();

         [XF] public void argument_3_is_null() =>
            Invoking(() => Asserter.NotNull3("a", "b", NullString)).Must().Throw<AssertionTestException>();
      }
   }

   public class called_with_4_arguments : NotNull_method
   {
      [XF] public void does_not_throw_if_all_are_non_null() =>
         Asserter.NotNull4("a", "b", "c", "d").Must().Be(Asserter);

      public class throws_if : called_with_4_arguments
      {
         [XF] public void argument_1_is_null() =>
            Invoking(() => Asserter.NotNull4(NullString, "b", "c", "d")).Must().Throw<AssertionTestException>();

         [XF] public void argument_2_is_null() =>
            Invoking(() => Asserter.NotNull4("a", NullString, "c", "d")).Must().Throw<AssertionTestException>();

         [XF] public void argument_3_is_null() =>
            Invoking(() => Asserter.NotNull4("a", "b", NullString, "d")).Must().Throw<AssertionTestException>();

         [XF] public void argument_4_is_null() =>
            Invoking(() => Asserter.NotNull4("a", "b", "c", NullString)).Must().Throw<AssertionTestException>();
      }
   }
}
