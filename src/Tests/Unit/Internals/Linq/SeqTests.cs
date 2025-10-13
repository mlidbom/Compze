using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Linq;

[TestFixture]
public class SeqTests : NUnitTestBase
{
   [Test]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      EnumerableCE.Create(oneToTen.ToArray()).Should().BeEquivalentTo(oneToTen);
   }
}