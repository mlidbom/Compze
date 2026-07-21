using Compze.Must;

using Compze.xUnitBDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.UnitConvert_class;

public static class Invoke_of_an_Action
{
   public class with_a_synchronous_Action
   {
      [XF] public void executes_the_Action_and_returns_unit()
      {
         var executed = false;
         var result = UnitConvert.Invoke(() => executed = true);
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }
}
