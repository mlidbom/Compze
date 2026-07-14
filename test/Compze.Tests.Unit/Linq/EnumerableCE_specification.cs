using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.Linq;


public class EnumerableCE_specification : UniversalTestBase
{
   [XF]
   public void UntilShouldHaveLastElementEqualToArgumentMinusStepSizeWhenSteppingByOneOreMinusOne()
   {
      1.Until(12).Last().Must().Be(12 - 1);
      1.By(1).Until(12).Last().Must().Be(12 - 1);
      (-1).By(-1).Until(-12).Last().Must().Be(-12 - -1);
   }

   [XF]
   public void UntilShouldStopEnumeratingAtValueBeforeGuard()
   {
      (-1).By(-2).Until(-7).Last().Must().Be(-5);
      (-1).By(2).Until(7).Last().Must().Be(5);

      (-2).By(3).Until(6).Last().Must().Be(4);
      2.By(3).Until(6).Last().Must().Be(5);
   }

   [XF]
   public void ThroughShouldHaveLastElementEqualToArgument()
   {
      1.Through(12).Last().Must().Be(12);
      1.By(1).Through(12).Last().Must().Be(12);
      (-1).By(-1).Through(-12).Last().Must().Be(-12);
   }

   [XF]
   public void ThroughShouldHaveCountEqualToToMinusFromPlus1()
   {
      12.Through(20).Count().Must().Be(20 - 12 + 1);
      12.By(1).Through(20).Count().Must().Be(20 - 12 + 1);
      (-12).By(-1).Through(-20).Count().Must().Be(20 - 12 + 1);
   }

   [XF]
   public void StepSizeShouldIterateFromImplicitParameter()
   {
      12.By(2).Through(int.MaxValue).First().Must().Be(12);
      (-12).By(-2).Through(-int.MaxValue).First().Must().Be(-12);
   }


   [XF]
   public void StepSizeShouldStepByStepsize()
   {
      12.By(2).Through(int.MaxValue).ElementAt(1).Must().Be(14);
      12.By(3).Through(int.MaxValue).ElementAt(1).Must().Be(15);
      12.By(3).Through(int.MaxValue).ElementAt(2).Must().Be(18);

      (-12).By(-2).Through(-int.MaxValue).ElementAt(1).Must().Be(-14);
      (-12).By(-3).Through(-int.MaxValue).ElementAt(1).Must().Be(-15);
      (-12).By(-3).Through(-int.MaxValue).ElementAt(2).Must().Be(-18);
   }
}
