using Compze.Utilities.Testing.XUnit.ComponentsPermutations;
using Xunit;

#pragma warning disable xUnit1026

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");
   }


   [ArgumentPassingTwoComponentsPCT] public void ComponentsPermutationCurrentIsAvailableInConstructor(ComponentsPermutation permutation) {}

   [ArgumentPassingTwoComponentsPCT] public void ThisIsTheCase(ComponentsPermutation permutation) =>
      _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase(ComponentsPermutation permutation) =>
         _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");
   }
}
