using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.Internals.Linq;


public class SeqTests : UniversalTestBase
{
   [XF]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10);
      EnumerableCE.Create(oneToTen.ToArray()).Must().SequenceEqual(oneToTen);
   }
}
