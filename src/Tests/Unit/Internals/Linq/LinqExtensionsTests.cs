using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.Internals.Linq;


public class LinqExtensionsTests : UniversalTestBase
{
   [XF]
   public void FlattenShouldIterateAllNestedCollectionInstances()
   {
      var nestedInts = new List<List<int>>
                       {
                          new() { 1 },
                          new() { 2, 3 },
                          new() { 4, 5, 6, 7 }
                       };

      var flattened = nestedInts.Flatten<List<int>, int>().ToList();
      flattened.Must().DeepEqual(1.Through(7).ToList());
   }

   [XF]
   public void ChoppingFollowedBySelectManyShouldEqualOriginalSequence()
   {
      var oneThroughAHundred = 1.Through(10003).ChopIntoSizesOf(10).SelectMany(me => me);
      oneThroughAHundred.Must().Equal(1.Through(10003));
   }

   [XF]
   public void ChoppingListIntoListSizeChunksShouldReturnOnlyOneChunk()
   {
      var oneEntry = 1.Through(10).ChopIntoSizesOf(10);
      oneEntry.Count().Must().Be(1);
   }
}
