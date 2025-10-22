using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.OneComponent.NotArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [NotArgumentPassingOneComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingOneComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");
   }
}
