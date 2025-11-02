using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using Compze.Utilities.Testing.Fluent;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeLessThan : UniversalTestBase
{
   public class with_a_value_greater_than_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_does_not_throw() => 3.Must().BeLessThan(5);
   }

   public class with_a_value_equal_to_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_throws() => Invoking(() => 5.Must().BeLessThan(5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_value_smaller_than_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_throws() => Invoking(() => 10.Must().BeLessThan(5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
