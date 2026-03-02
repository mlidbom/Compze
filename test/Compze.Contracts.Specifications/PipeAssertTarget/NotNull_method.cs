using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Contracts.Exceptions.AssertionFailedException;

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.PipeAssertTarget;

public class NotNull_method
{
   static readonly string? NullString = null;

   public class called_on_null_value : NotNull_method
   {
      [XF] public void throws_AssertionFailedException() =>
         Invoking(() => NullString._assert().NotNull()).Must().Throw<AssertionFailedException>();

      [XF] public void exception_message_contains_the_value_expression() =>
         Invoking(() => NullString._assert().NotNull())
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain(nameof(NullString));
   }

   public class called_on_non_null_value : NotNull_method
   {
      [XF] public void returns_the_value() =>
         "hello"._assert().NotNull().Must().Be("hello");
   }
}
