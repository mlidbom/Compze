using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_BeGreaterThan : UniversalTestBase
{
   public class with_a_value_smaller_value_than_the_actual_value : When_calling_Must_BeGreaterThan
   {
      [XF] public void it_does_not_throw() => 10.Must().BeGreaterThan(5);
   }

   public class with_a_value_equal_to_the_actual_value : When_calling_Must_BeGreaterThan
   {
      [XF] public void it_throws() => Invoking(() => 5.Must().BeGreaterThan(5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_value_greater_than_the_actual_value : When_calling_Must_BeGreaterThan
   {
      [XF] public void it_throws() => Invoking(() => 3.Must().BeGreaterThan(5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
