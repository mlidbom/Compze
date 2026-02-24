using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Functional.Specifications.ObjectCE;

public class _repeat_method
{
   [XF] public void returns_value_repeated_specified_number_of_times() =>
      "x"._repeat(3).Must().HaveCount(3);

   [XF] public void all_elements_equal_the_original_value()
   {
      foreach(var item in 42._repeat(5))
      {
         item.Must().Be(42);
      }
   }

   [XF] public void returns_empty_sequence_when_times_is_zero() =>
      "x"._repeat(0).Must().HaveCount(0);

   [XF] public void returns_empty_sequence_when_times_is_negative() =>
      "x"._repeat(-1).Must().HaveCount(0);
}
