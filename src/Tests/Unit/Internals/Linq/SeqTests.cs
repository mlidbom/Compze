using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Linq;


public class SeqTests : UniversalTestBase
{
   [XF]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      EnumerableCE.Create(oneToTen.ToArray()).Should().BeEquivalentTo(oneToTen);
   }
}