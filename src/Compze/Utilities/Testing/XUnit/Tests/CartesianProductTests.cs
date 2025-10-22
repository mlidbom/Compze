using FluentAssertions;
using Compze.Utilities.SystemCE.LinqCE;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests;

public class CartesianProductTests
{
   [Fact]
   public void EmptyInput_ReturnsListWithEmptyList()
   {
      var emptyLists = new List<IReadOnlyList<string>>();

      var result = emptyLists.CartesianProduct();

      result.Should().HaveCount(1);
      result[0].Should().BeEmpty();
   }

   [Fact]
   public void SingleListWithOneItem_ReturnsThatItemAlone()
   {
      var lists = new List<IReadOnlyList<string>>
                  {
                     new List<string> { "A" }
                  };

      var result = lists.CartesianProduct();

      result.Should().HaveCount(1);
      result[0].Should().Equal("A");
   }

   [Fact]
   public void SingleListWithTwoItems_ReturnsTwoSingleItemCombinations()
   {
      var lists = new List<IReadOnlyList<string>>
                  {
                     new List<string> { "A", "B" }
                  };

      var result = lists.CartesianProduct();

      result.Should().HaveCount(2);
      result[0].Should().Equal("A");
      result[1].Should().Equal("B");
   }

   [Fact]
   public void TwoListsWithTwoItemsEach_ReturnsFourCombinations() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A", "B" },
         new List<string> { "X", "Y" }
      }.CartesianProduct().Should().BeEquivalentTo(
         new List<IReadOnlyList<string>>
         {
            new List<string> { "A", "X" },
            new List<string> { "A", "Y" },
            new List<string> { "B", "X" },
            new List<string> { "B", "Y" }
         });

   [Fact]
   public void ThreeListsWithDifferentSizes_ReturnsCorrectNumberOfCombinations() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A", "B" },     // 2 items
         new List<string> { "X" },          // 1 item
         new List<string> { "1", "2", "3" } // 3 items
      }.CartesianProduct().Should().BeEquivalentTo(
         new List<IReadOnlyList<string>>
         {
            new List<string> { "A", "X", "1" },
            new List<string> { "A", "X", "2" },
            new List<string> { "A", "X", "3" },
            new List<string> { "B", "X", "1" },
            new List<string> { "B", "X", "2" },
            new List<string> { "B", "X", "3" }
         });

   [Fact]
   public void OneListContainsEmptyList_ReturnsEmpty() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A", "B" },
         new List<string>(), // Empty!
         new List<string> { "X", "Y" }
      }.CartesianProduct().Should().BeEmpty();
}
