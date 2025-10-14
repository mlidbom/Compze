using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.LinqCE;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.XUnit.Linq;


public class LinqExtensionsTests : XUnitTestBase
{
   [Fact]
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

   [Fact]
   public void ChoppingFollowedBySelectManyShouldEqualOriginalSequence()
   {
      var oneThroughAHundred = 1.Through(10003).ChopIntoSizesOf(10).SelectMany(me => me);
      oneThroughAHundred.Should().Equal(1.Through(10003));
   }

   [Fact]
   public void ChoppingListIntoListSizeChunksShouldReturnOnlyOneChunk()
   {
      var oneEntry = 1.Through(10).ChopIntoSizesOf(10);
      oneEntry.Count().Should().Be(1);
   }
}