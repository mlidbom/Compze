using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.OneComponent.ArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [ArgumentPassingOneComponentsPCT] public void ThisIsTheCase(ComponentsPermutation permutation) =>
      _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingOneComponentsPCT] public void ThisIsAlsoTheCase(ComponentsPermutation permutation) =>
         _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}");
   }
}
