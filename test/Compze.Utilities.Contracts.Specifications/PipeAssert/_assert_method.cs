using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Utilities.Contracts.Specifications.PipeAssert;

public class _assert_method
{
   public class with_default_message
   {
      [XF] public void returns_value_when_predicate_is_true() =>
         42._assert(x => x > 0).Must().Be(42);

      [XF] public void throws_when_predicate_is_false() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>();

      [XF] public void exception_message_contains_predicate_expression() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("x => x > 0");

      [XF] public void exception_message_contains_the_value() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("-1");
   }

   public class with_message_factory
   {
      [XF] public void returns_value_when_predicate_is_true() =>
         42._assert(x => x > 0, x => $"Expected positive but got {x}").Must().Be(42);

      [XF] public void throws_when_predicate_is_false() =>
         Invoking(() => (-1)._assert(x => x > 0, x => $"Expected positive but got {x}"))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("Expected positive but got -1");
   }

   public class with_exception_factory
   {
      [XF] public void returns_value_when_predicate_is_true() =>
         42._assert(x => x > 0, () => new InvalidOperationException("fail")).Must().Be(42);

      [XF] public void throws_specified_exception_when_predicate_is_false() =>
         Invoking(() => (-1)._assert(x => x > 0, () => new InvalidOperationException("negative!")))
            .Must().Throw<InvalidOperationException>()
            .Which.Message.Must().Contain("negative!");
   }
}
