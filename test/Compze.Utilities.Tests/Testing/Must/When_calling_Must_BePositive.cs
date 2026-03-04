using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Must;

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
