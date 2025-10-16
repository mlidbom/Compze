using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.Linq;


public class LinqExtensionsTests : XUnitTestBase
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
      flattened.Should().BeEquivalentTo(1.Through(7));
   }

   [XF]
   public void ChoppingFollowedBySelectManyShouldEqualOriginalSequence()
   {
      var oneThroughAHundred = 1.Through(10003).ChopIntoSizesOf(10).SelectMany(me => me);
      oneThroughAHundred.Should().Equal(1.Through(10003));
   }

   [XF]
   public void ChoppingListIntoListSizeChunksShouldReturnOnlyOneChunk()
   {
      var oneEntry = 1.Through(10).ChopIntoSizesOf(10);
      oneEntry.Count().Should().Be(1);
   }
}