using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Underscore.Specifications.Pipe;

public static class Then_operator
{
   public class with_constant_value
   {
      [XF] public void returns_the_provided_value_ignoring_the_piped_value() =>
         "ignored"._(42).Must().Be(42);
   }

   public class with_func
   {
      [XF] public void returns_the_func_result_ignoring_the_piped_value() =>
         "ignored"._((() => 42)).Must().Be(42);

      [XF] public void the_func_is_invoked()
      {
         var invoked = false;
         "ignored"._((() => { invoked = true; return 0; }));
         invoked.Must().BeTrue();
      }
   }

   public class with_action
   {
      [XF] public void executes_the_action_and_returns_unit()
      {
         var executed = false;
         var result = "ignored"._((Action)(() => { executed = true; }));
         executed.Must().BeTrue();
         result.Must().Be(unit);
      }

      [XF] public void the_return_value_is_unit() =>
         "ignored"._((Action)(() => { })).Must().Be(unit);
   }
}
