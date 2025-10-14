using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.XUnit.Linq;


public class SeqTests : XUnitTestBase
{
   [Fact]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      EnumerableCE.Create(oneToTen.ToArray()).Should().BeEquivalentTo(oneToTen);
   }
}