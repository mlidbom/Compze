using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Linq;

[TestFixture]
public class EnumerableCE_specification : UniversalTestBase
{
   [Test]
   public void UntilShouldHaveLastElementEqualToArgumentMinusStepSizeWhenSteppingByOneOreMinusOne()
   {
      1.Until(12).Last().Should().Be(12 - 1);
      1.By(1).Until(12).Last().Should().Be(12 - 1);
      (-1).By(-1).Until(-12).Last().Should().Be(-12 - -1);
   }

   [Test]
   public void UntilShouldStopEnumeratingAtValueBeforeGuard()
   {
      (-1).By(-2).Until(-7).Last().Should().Be(-5);
      (-1).By(2).Until(7).Last().Should().Be(5);

      (-2).By(3).Until(6).Last().Should().Be(4);
      2.By(3).Until(6).Last().Should().Be(5);
   }

   [Test]
   public void ThroughShouldHaveLastElementEqualToArgument()
   {
      1.Through(12).Last().Should().Be(12);
      1.By(1).Through(12).Last().Should().Be(12);
      (-1).By(-1).Through(-12).Last().Should().Be(-12);
   }

   [Test]
   public void ThroughShouldHaveCountEqualToToMinusFromPlus1()
   {
      12.Through(20).Count().Should().Be(20 - 12 + 1);
      12.By(1).Through(20).Count().Should().Be(20 - 12 + 1);
      (-12).By(-1).Through(-20).Count().Should().Be(20 - 12 + 1);
   }

   [Test]
   public void StepSizeShouldIterateFromImplicitParameter()
   {
      12.By(2).Through(int.MaxValue).First().Should().Be(12);
      (-12).By(-2).Through(-int.MaxValue).First().Should().Be(-12);
   }


   [Test]
   public void StepSizeShouldStepByStepsize()
   {
      12.By(2).Through(int.MaxValue).ElementAt(1).Should().Be(14);
      12.By(3).Through(int.MaxValue).ElementAt(1).Should().Be(15);
      12.By(3).Through(int.MaxValue).ElementAt(2).Should().Be(18);

      (-12).By(-2).Through(-int.MaxValue).ElementAt(1).Should().Be(-14);
      (-12).By(-3).Through(-int.MaxValue).ElementAt(1).Should().Be(-15);
      (-12).By(-3).Through(-int.MaxValue).ElementAt(2).Should().Be(-18);
   }
}