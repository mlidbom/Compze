using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Functional.Specifications.Pipe;

public class Then_operator
{
   public class with_constant_value
   {
      [XF] public void returns_the_provided_value_ignoring_the_piped_value() =>
         "ignored"._(42).Must().Be(42);
   }

   public class with_func
   {
      [XF] public void returns_the_func_result_ignoring_the_piped_value() =>
         "ignored"._(() => 42).Must().Be(42);

      [XF] public void the_func_is_invoked()
      {
         var invoked = false;
         "ignored"._(() => { invoked = true; return 0; });
         invoked.Must().BeTrue();
      }
   }

}
