// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_BeLessThanOrEqualTo : UniversalTestBase
{
   public class with_a_value_greater_than_the_actual_value : When_calling_Must_BeLessThanOrEqualTo
   {
      [XF] public void it_does_not_throw() => 3.Must().BeLessThanOrEqualTo(5);
   }

   public class with_a_value_equal_to_the_actual_value : When_calling_Must_BeLessThanOrEqualTo
   {
      [XF] public void it_does_not_throw() => 5.Must().BeLessThanOrEqualTo(5);
   }

   public class with_a_value_smaller_than_the_actual_value : When_calling_Must_BeLessThanOrEqualTo
   {
      [XF] public void it_throws() => Invoking(() => 10.Must().BeLessThanOrEqualTo(5))
                                     .Must()
                                     .Throw<AssertionFailedException>();
   }
}
