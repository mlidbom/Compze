using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___IComparableAssertions = Compze.Utilities.Testing.Fluent.Must___IComparableAssertions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BeLessThan : UniversalTestBase
{
   public class with_a_value_greater_than_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_does_not_throw() => Must___IComparableAssertions.BeLessThan(__Must.Must(3), 5);
   }

   public class with_a_value_equal_to_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_throws() => Invoking(() => Must___IComparableAssertions.BeLessThan(__Must.Must(5), 5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_value_smaller_than_the_actual_value : When_calling_Must_BeLessThan
   {
      [XF] public void it_throws() => Invoking(() => Must___IComparableAssertions.BeLessThan(__Must.Must(10), 5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
