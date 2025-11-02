using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using Compze.Utilities.Testing.Fluent;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_BePositive : UniversalTestBase
{
   public class with_a_positive_number : When_calling_Must_BePositive
   {
      [XF] public void it_does_not_throw() => 42.Must().BePositive();
   }

   public class with_zero : When_calling_Must_BePositive
   {
      [XF] public void it_throws() => Invoking(() => 0.Must().BePositive())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_negative_number : When_calling_Must_BePositive
   {
      [XF] public void it_throws() => Invoking(() => (-5).Must().BePositive())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
