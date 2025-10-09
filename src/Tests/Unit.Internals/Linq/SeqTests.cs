using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Unit.Internals.Linq;

[TestFixture]
public class SeqTests : UniversalTestBase
{
   [Test]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      Assert.That(EnumerableCE.Create(oneToTen.ToArray()), Is.EquivalentTo(oneToTen));
   }
}