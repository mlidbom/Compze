using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.Internals.Linq;


public class SeqTests : UniversalTestBase
{
   [XF]
   public void CreateShouldEnumerateAllParamsInOrder()
   {
      var oneToTen = 1.Through(10).ToList();
      EnumerableCE.Create(oneToTen.ToArray()).Must().SequenceEqual(oneToTen);
   }
}
