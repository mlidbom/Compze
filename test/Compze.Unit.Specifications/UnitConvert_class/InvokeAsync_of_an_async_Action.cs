using Compze.Must;

using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class InvokeAsync_of_an_async_Action
{
   public class with_an_async_Action
   {
      [XF] public async Task awaits_the_async_Action_and_returns_unit()
      {
         var executed = false;
         var result = await UnitConvert.InvokeAsync(async () =>
         {
            await Task.Yield();
            executed = true;
         });
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }
}
