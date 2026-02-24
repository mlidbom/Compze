using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Functional.Specifications.Pipe;

public class _then_method
{
   public class with_constant_value
   {
      [XF] public void ignores_previous_value_and_returns_constant() =>
         "ignored"._then(42).Must().Be(42);
   }

   public class with_func
   {
      [XF] public void ignores_previous_value_and_returns_func_result() =>
         "ignored"._then(() => 42).Must().Be(42);

      [XF] public void invokes_the_func()
      {
         var invoked = false;
         "ignored"._then(() => { invoked = true; return 0; });
         invoked.Must().BeTrue();
      }
   }

   public class with_action
   {
      [XF] public void ignores_previous_value_and_executes_action()
      {
         var executed = false;
         var result = "ignored"._then(() => { executed = true; });
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }

      [XF] public void returns_unit() =>
         "ignored"._then(() => { }).Must().Be(unit.Value);
   }
}
