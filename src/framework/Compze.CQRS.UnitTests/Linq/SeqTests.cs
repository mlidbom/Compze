using System.Linq;
using Composable.SystemCE.LinqCE;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.Tests.Linq;

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