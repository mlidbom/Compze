using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._1Component.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");
   }


   [ArgumentPassingOneComponentsPCT] public void ComponentsPermutationCurrentIsAvailableInConstructor(ComponentsPermutation permutation) {}

   [ArgumentPassingOneComponentsPCT] public void ThisIsTheCase(ComponentsPermutation permutation) =>
      _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingOneComponentsPCT] public void ThisIsAlsoTheCase(ComponentsPermutation permutation) =>
         _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");
   }
}
