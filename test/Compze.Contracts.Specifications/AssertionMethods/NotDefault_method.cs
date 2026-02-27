using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods;

public class NotDefault_method : AssertionMethodsTest
{
   static readonly Guid DefaultGuid = default;

   public class called_with_default_Guid : NotDefault_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotDefault(DefaultGuid)).Must().Throw<AssertionTestException>();

      [XF] public void exception_message_contains_the_argument_expression() =>
         Invoking(() => Asserter.NotDefault(DefaultGuid))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultGuid));
   }

   public class called_with_non_default_value : NotDefault_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotDefault(Guid.NewGuid()).Must().Be(Asserter);
   }

   public class called_with_two_values_where_first_is_default : NotDefault_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotDefault(default(Guid), Guid.NewGuid())).Must().Throw<AssertionTestException>();
   }

   public class called_with_two_values_where_second_is_default : NotDefault_method
   {
      [XF] public void throws() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), default(Guid))).Must().Throw<AssertionTestException>();
   }

   public class called_with_two_non_default_values : NotDefault_method
   {
      [XF] public void returns_the_asserter_for_chaining() =>
         Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid()).Must().Be(Asserter);
   }
}
