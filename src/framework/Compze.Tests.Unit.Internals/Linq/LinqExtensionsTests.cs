using System.Collections.Generic;
using System.Linq;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals.Linq;

[TestFixture]
public class LinqExtensionsTests : UniversalTestBase
{
   [Test]
   public void FlattenShouldIterateAllNestedCollectionInstances()
   {
      var nestedInts = new List<List<int>>
                       {
                          new() { 1 },
                          new() { 2, 3 },
                          new() { 4, 5, 6, 7 }
                       };

      var flattened = nestedInts.Flatten<List<int>, int>();
      Assert.That(flattened, Is.EquivalentTo(1.Through(7)));
   }

   [Test]
   public void ChoppingFollowedBySelectManyShouldEqualOriginalSequence()
   {
      var oneThroughAHundred = 1.Through(10003).ChopIntoSizesOf(10).SelectMany(me => me);
      Assert.That(oneThroughAHundred, Is.EqualTo(1.Through(10003)));
   }

   [Test]
   public void ChoppingListIntoListSizeChunksShouldReturnOnlyOneChunk()
   {
      var oneEntry = 1.Through(10).ChopIntoSizesOf(10);
      Assert.That(oneEntry.Count(), Is.EqualTo(1));
   }
}