using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.ArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [ArgumentPassingTwoComponentsPCT] public void ThisIsTheCase(ComponentsPermutation permutation) =>
      _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}, SqlLayer enum: {permutation.SqlLayer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase(ComponentsPermutation permutation) =>
         _testOutputHelper.WriteLine($"Serializer enum: {permutation.Serializer()}, SqlLayer enum: {permutation.SqlLayer()}");
   }
}
