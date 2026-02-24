using System;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Functional.Specifications.ActionFuncConverter;

public class AsAction_from_Func
{
   public class with_zero_parameters
   {
      [XF] public void executes_the_func()
      {
         var executed = false;
         Func<unit> func = () => { executed = true; return unit.Value; };
         func.AsAction()();
         executed.Must().BeTrue();
      }
   }

   public class with_one_parameter
   {
      [XF] public void executes_the_func_with_parameter()
      {
         var captured = "";
         Func<string, unit> func = s => { captured = s; return unit.Value; };
         func.AsAction()("hello");
         captured.Must().Be("hello");
      }
   }

   public class with_two_parameters
   {
      [XF] public void executes_the_func_with_parameters()
      {
         var capturedA = "";
         var capturedB = 0;
         Func<string, int, unit> func = (s, i) => { capturedA = s; capturedB = i; return unit.Value; };
         func.AsAction()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
      }
   }
}
