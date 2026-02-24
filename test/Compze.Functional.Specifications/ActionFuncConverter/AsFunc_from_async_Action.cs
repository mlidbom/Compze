using System;
using System.Threading.Tasks;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Functional.Specifications.ActionFuncConverter;

public class AsFunc_from_async_Action
{
   public class with_zero_parameters
   {
      [XF] public async Task executes_the_async_action_and_returns_unit()
      {
         var executed = false;
         Func<Task> action = async () => { await Task.Yield(); executed = true; };
         var result = await action.AsFunc()();
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }
   }

   public class with_one_parameter
   {
      [XF] public async Task executes_the_async_action_with_parameter_and_returns_unit()
      {
         var captured = "";
         Func<string, Task> action = async s => { await Task.Yield(); captured = s; };
         var result = await action.AsFunc()("hello");
         captured.Must().Be("hello");
         result.Must().Be(unit.Value);
      }
   }

   public class with_two_parameters
   {
      [XF] public async Task executes_the_async_action_with_parameters_and_returns_unit()
      {
         var capturedA = "";
         var capturedB = 0;
         Func<string, int, Task> action = async (s, i) => { await Task.Yield(); capturedA = s; capturedB = i; };
         var result = await action.AsFunc()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
         result.Must().Be(unit.Value);
      }
   }
}
