using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class AsFunc_from_Action
{
   public class with_zero_parameters
   {
         [XF] public void the_converted_Func_executes_the_Action_and_returns_unit()
      {
         var executed = false;
         Action action = () => executed = true;
         var result = action.ToFunc()();
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }

   public class with_one_parameter
   {
         [XF] public void the_converted_Func_passes_the_parameter_to_the_Action()
      {
         var captured = "";
         Action<string> action = s => captured = s;
         var result = action.ToFunc()("hello");
         captured.Must().Be("hello");
         result.Must().Be(Unit.Value);
      }
   }

   public class with_two_parameters
   {
         [XF] public void the_converted_Func_passes_both_parameters_to_the_Action()
      {
         var capturedA = "";
         var capturedB = 0;
         Action<string, int> action = (s, i) => { capturedA = s; capturedB = i; };
         var result = action.ToFunc()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
         result.Must().Be(Unit.Value);
      }
   }
}
