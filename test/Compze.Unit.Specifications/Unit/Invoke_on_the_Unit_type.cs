using Compze.Must;

using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

// ReSharper disable once CheckNamespace Folder is 'Unit', but a '.Unit' namespace segment would collide with the Unit type under test (Unit.Invoke).
namespace Compze.UnitSpecifications;

public static class Invoke_on_the_Unit_type
{
   public class Invoke_with_an_Action
   {
      [XF] public void executes_the_Action_and_returns_unit()
      {
         var executed = false;
         var result = Unit.Invoke(() => executed = true);
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }

   public class InvokeAsync_with_an_async_Action
   {
      [XF] public async Task awaits_the_async_Action_and_returns_unit()
      {
         var executed = false;
         var result = await Unit.InvokeAsync(async () =>
         {
            await Task.Yield();
            executed = true;
         });
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }
}
