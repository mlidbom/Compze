using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class AsAsyncFunc_from_async_Action
{
   public class with_zero_parameters
   {
         [XF] public async Task the_converted_Func_executes_the_async_Action_and_returns_unit()
      {
         var executed = false;
         Func<Task> action = async () => { await Task.Yield(); executed = true; };
         var result = await action.ToAsyncFunc()();
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }

   public class with_one_parameter
   {
         [XF] public async Task the_converted_Func_passes_the_parameter_to_the_async_Action()
      {
         var captured = "";
         Func<string, Task> action = async s => { await Task.Yield(); captured = s; };
         var result = await action.ToAsyncFunc()("hello");
         captured.Must().Be("hello");
         result.Must().Be(Unit.Value);
      }
   }

   public class with_two_parameters
   {
         [XF] public async Task the_converted_Func_passes_both_parameters_to_the_async_Action()
      {
         var capturedA = "";
         var capturedB = 0;
         Func<string, int, Task> action = async (s, i) => { await Task.Yield(); capturedA = s; capturedB = i; };
         var result = await action.ToAsyncFunc()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
         result.Must().Be(Unit.Value);
      }
   }
}
