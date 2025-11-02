using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Tests.Infrastructure.Fluent;

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

      var flattened = nestedInts.Flatten<List<int>, int>();
      flattened.Must().DeepEqual(1.Through(7));
   }

   [XF]
   public void ChoppingFollowedBySelectManyShouldEqualOriginalSequence()
   {
      var oneThroughAHundred = 1.Through(10003).ChopIntoSizesOf(10).SelectMany(me => me);
      oneThroughAHundred.Must().SequenceEqual(1.Through(10003));
   }

   [XF]
   public void ChoppingListIntoListSizeChunksShouldReturnOnlyOneChunk()
   {
      var oneEntry = 1.Through(10).ChopIntoSizesOf(10);
      oneEntry.Count().Must().Be(1);
   }
}
