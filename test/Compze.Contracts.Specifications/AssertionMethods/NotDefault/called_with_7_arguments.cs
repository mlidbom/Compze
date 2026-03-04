using Compze.Must;
using Compze.xUnit.BDD;
// ReSharper disable PreferConcreteValueOverDefault

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.AssertionMethods.NotDefault;

public class called_with_7_arguments : AssertionMethodsTest
{
   [XF] public void does_not_throw_if_all_are_non_default() =>
      Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Must().Be(Asserter);

   public class throws_with_message_containing_the_failing_argument_expression_if : called_with_7_arguments
   {
      static readonly Guid DefaultArg1 = default;
      static readonly Guid DefaultArg2 = default;
      static readonly Guid DefaultArg3 = default;
      static readonly Guid DefaultArg4 = default;
      static readonly Guid DefaultArg5 = default;
      static readonly Guid DefaultArg6 = default;
      static readonly Guid DefaultArg7 = default;

      [XF] public void argument_1_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(DefaultArg1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), nameof(DefaultArg1));

      [XF] public void argument_2_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), DefaultArg2, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), nameof(DefaultArg2));

      [XF] public void argument_3_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), DefaultArg3, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), nameof(DefaultArg3));

      [XF] public void argument_4_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg4, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), nameof(DefaultArg4));

      [XF] public void argument_5_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg5, Guid.NewGuid(), Guid.NewGuid()), nameof(DefaultArg5));

      [XF] public void argument_6_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg6, Guid.NewGuid()), nameof(DefaultArg6));

      [XF] public void argument_7_is_default() =>
         MustThrowContaining(() => Asserter.NotDefault(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DefaultArg7), nameof(DefaultArg7));
   }
}