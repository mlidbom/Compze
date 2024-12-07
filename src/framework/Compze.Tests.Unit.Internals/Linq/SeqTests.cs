using System.Linq;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using NUnit.Framework;

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