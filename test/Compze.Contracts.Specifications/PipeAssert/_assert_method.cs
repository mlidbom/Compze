using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Contracts.Specifications.PipeAssert;

public class _assert_method
{
   public class called_with_only_a_predicate
   {
      [XF] public void returns_the_piped_value_when_the_predicate_passes() =>
         42._assert(x => x > 0).Must().Be(42);

      [XF] public void throws_AssertionFailedException_when_the_predicate_fails() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>();

      [XF] public void the_exception_message_contains_the_predicate_source_expression() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("x => x > 0");

      [XF] public void the_exception_message_contains_the_asserted_value() =>
         Invoking(() => (-1)._assert(x => x > 0))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("-1");
   }

   public class called_with_a_message_factory
   {
      [XF] public void returns_the_piped_value_when_the_predicate_passes() =>
         42._assert(x => x > 0, x => $"Expected positive but got {x}").Must().Be(42);

      [XF] public void throws_with_the_factory_produced_message_when_the_predicate_fails() =>
         Invoking(() => (-1)._assert(x => x > 0, x => $"Expected positive but got {x}"))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("Expected positive but got -1");
   }

   public class called_with_an_exception_factory
   {
      [XF] public void returns_the_piped_value_when_the_predicate_passes() =>
         42._assert(x => x > 0, () => new InvalidOperationException("fail")).Must().Be(42);

      [XF] public void throws_the_exception_from_the_factory_when_the_predicate_fails() =>
         Invoking(() => (-1)._assert(x => x > 0, () => new InvalidOperationException("negative!")))
            .Must().Throw<InvalidOperationException>()
            .Which.Message.Must().Contain("negative!");
   }
}
