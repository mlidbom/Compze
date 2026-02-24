using System;
using System.Threading.Tasks;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Utilities.Functional.Specifications.ActionFuncConverter;

public class AsFunc_from_Action
{
   public class with_zero_parameters
   {
      [XF] public void executes_the_action_and_returns_unit()
      {
         var executed = false;
         Action action = () => executed = true;
         var result = action.AsFunc()();
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }
   }

   public class with_one_parameter
   {
      [XF] public void executes_the_action_with_parameter_and_returns_unit()
      {
         var captured = "";
         Action<string> action = s => captured = s;
         var result = action.AsFunc()("hello");
         captured.Must().Be("hello");
         result.Must().Be(unit.Value);
      }
   }

   public class with_two_parameters
   {
      [XF] public void executes_the_action_with_parameters_and_returns_unit()
      {
         var capturedA = "";
         var capturedB = 0;
         Action<string, int> action = (s, i) => { capturedA = s; capturedB = i; };
         var result = action.AsFunc()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
         result.Must().Be(unit.Value);
      }
   }
}
