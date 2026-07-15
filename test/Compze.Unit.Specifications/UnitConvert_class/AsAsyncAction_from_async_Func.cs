using Compze.Must;

using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class AsAsyncAction_from_async_Func
{
   public class with_zero_parameters
   {
      [XF] public async Task the_converted_async_Action_invokes_the_async_Func()
      {
         var executed = false;
         Func<Task<Unit>> func = async () => { await Task.Yield(); executed = true; return unit; };
         await func.ToAsyncAction()();
         executed.Must().BeTrue();
      }
   }

   public class with_one_parameter
   {
      [XF] public async Task the_converted_async_Action_passes_the_parameter_to_the_async_Func()
      {
         var captured = "";
         Func<string, Task<Unit>> func = async s => { await Task.Yield(); captured = s; return unit; };
         await func.ToAsyncAction()("hello");
         captured.Must().Be("hello");
      }
   }

   public class with_two_parameters
   {
      [XF] public async Task the_converted_async_Action_passes_both_parameters_to_the_async_Func()
      {
         var capturedA = "";
         var capturedB = 0;
         Func<string, int, Task<Unit>> func = async (s, i) => { await Task.Yield(); capturedA = s; capturedB = i; return unit; };
         await func.ToAsyncAction()("hello", 42);
         capturedA.Must().Be("hello");
         capturedB.Must().Be(42);
      }
   }
}
