using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.Must;
using Xunit;

#pragma warning disable CA1861 //we want the collections inline

namespace Compze.Utilities.Tests.SystemCE.LinqCE;

public class CartesianProductTests
{
   [Fact]
   public void EmptyInput_ReturnsListWithEmptyList() =>
      new List<IReadOnlyList<string>>()
        .CartesianProduct().Must().DeepEqual(
            new List<IReadOnlyList<string>>
            {
               Array.Empty<string>(),
            },
            config => config.IgnoreTypes());

   [Fact]
   public void SingleListWithOneItem_ReturnsThatItemAlone() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A" }
      }.CartesianProduct().Must().DeepEqual(
         new List<IReadOnlyList<string>>
         {
            new[] { "A" }
         });

   [Fact]
   public void SingleListWithTwoItems_ReturnsTwoSingleItemCombinations() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A", "B" }
      }.CartesianProduct().Must().DeepEqual(
         new List<IReadOnlyList<string>>
         {
            new[] { "A" },
            new[] { "B" }
         });

   [Fact]
   public void TwoListsWithTwoItemsEach_ReturnsFourCombinations() =>
      new List<IReadOnlyList<string>>
      {
         new List<string> { "A", "B" },
         new List<string> { "X", "Y" }
      }.CartesianProduct().Must().DeepEqual(
         new List<IReadOnlyList<string>>
         {
            new[] { "A", "X" },
            new[] { "A", "Y" },
            new[] { "B", "X" },
            new[] { "B", "Y" }
         });

   [Fact]
   public void ThreeListsWithDifferentSizes_ReturnsCorrectNumberOfCombinations() =>
      new List<string[]>
      {
         new[]  { "A", "B" },     // 2 items
         new[]  { "X" },          // 1 item
         new[]  { "1", "2", "3" } // 3 items
      }.CartesianProduct().Must().DeepEqual(
         new List<IReadOnlyList<string>>
         {
            new[] { "A", "X", "1" },
            new[] { "A", "X", "2" },
            new[] { "A", "X", "3" },
            new[] { "B", "X", "1" },
            new[] { "B", "X", "2" },
            new[] { "B", "X", "3" }
         });

   [Fact]
   public void OneListContainsEmptyList_ReturnsEmpty() =>
      new List<IReadOnlyList<string>>
      {
         new[]  { "A", "B" },
         Array.Empty<string>(), // Empty!
         new[]  { "X", "Y" }
      }.CartesianProduct().Must().BeEmpty();
}
