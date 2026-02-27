using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotDefault;

public class called_with_5_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_default() =>
      Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Must().Be(Asserter);

   public class throws_if : called_with_5_arguments
   {
      [XF] public void argument_1_is_default() =>
         Invoking(() => Asserter.NotDefault(default(Guid), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_2_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), default(Guid), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_3_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), default(Guid), Guid.NewGuid(), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_4_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default(Guid), Guid.NewGuid())).Must().Throw<AssertionTestException>();

      [XF] public void argument_5_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default(Guid))).Must().Throw<AssertionTestException>();
   }

   public class exception_message_contains_the_argument_expression_if : called_with_5_arguments
   {
      static readonly Guid DefaultArg1 = default;
      static readonly Guid DefaultArg2 = default;
      static readonly Guid DefaultArg3 = default;
      static readonly Guid DefaultArg4 = default;
      static readonly Guid DefaultArg5 = default;

      [XF] public void argument_1_is_default() =>
         Invoking(() => Asserter.NotDefault(DefaultArg1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultArg1));

      [XF] public void argument_2_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), DefaultArg2, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultArg2));

      [XF] public void argument_3_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), DefaultArg3, Guid.NewGuid(), Guid.NewGuid()))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultArg3));

      [XF] public void argument_4_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg4, Guid.NewGuid()))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultArg4));

      [XF] public void argument_5_is_default() =>
         Invoking(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg5))
            .Must().Throw<AssertionTestException>()
            .Which.Message.Must().Contain(nameof(DefaultArg5));
   }
}
