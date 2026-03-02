using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Contracts.Exceptions.AssertionFailedException;
// ReSharper disable PreferConcreteValueOverDefault

#pragma warning disable CA1052

namespace Compze.Contracts.Specifications.PipeAssertTarget;

public class NotDefault_method
{
   static readonly Guid DefaultGuid = default;

   public class called_on_default_value : NotDefault_method
   {
      [XF] public void throws_AssertionFailedException() =>
         Invoking(() => DefaultGuid._assert().NotDefault()).Must().Throw<AssertionFailedException>();

      [XF] public void exception_message_contains_the_value_expression() =>
         Invoking(() => DefaultGuid._assert().NotDefault())
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain(nameof(DefaultGuid));
   }

   public class called_on_non_default_value : NotDefault_method
   {
      static readonly Guid ValidGuid = Guid.NewGuid();

      [XF] public void returns_the_value() =>
         ValidGuid._assert().NotDefault().Must().Be(ValidGuid);
   }
}
