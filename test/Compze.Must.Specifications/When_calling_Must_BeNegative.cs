// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_BeNegative : UniversalTestBase
{
   public class with_a_negative_number : When_calling_Must_BeNegative
   {
      [XF] public void it_does_not_throw() => (-5).Must().BeNegative();
   }

   public class with_zero : When_calling_Must_BeNegative
   {
      [XF] public void it_throws() => Invoking(() => 0.Must().BeNegative())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }

   public class with_a_positive_number : When_calling_Must_BeNegative
   {
      [XF] public void it_throws() => Invoking(() => 42.Must().BeNegative())
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
