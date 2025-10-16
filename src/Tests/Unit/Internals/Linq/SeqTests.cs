using System.Linq;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Linq;


public class SeqTests : XUnitTestBase
{
   [XF]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      EnumerableCE.Create(oneToTen.ToArray()).Should().BeEquivalentTo(oneToTen);
   }
}