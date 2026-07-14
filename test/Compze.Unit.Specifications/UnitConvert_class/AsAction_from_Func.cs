using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class AsAction_from_Func
{
   public class with_zero_parameters
   {
      [XF] public void the_converted_Action_invokes_the_Func()
      {
         var executed = false;
         Func<Unit> func = () => { executed = true; return unit; };
         func.ToAction()();
         executed.Must().BeTrue();
      }
   }

   public class with_one_parameter
   {
      [XF] public void the_converted_Action_passes_the_parameter_to_the_Func()
      {
         var captured = "";
         Func<string, Unit> func = s => { captured = s; return unit; };
         func.ToAction()("hello");
         captured.Must().Be("hello");
      }
   }

   public class with_two_parameters
   {
         [XF] public void the_converted_Action_passes_both_parameters_to_the_Func()
      {
         var capturedA = "";
         var capturedB = 0;
         Func<string, int, Unit> func = (s, i) => { capturedA = s; capturedB = i; return unit; };
         func.ToAction()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
      }
   }
}
