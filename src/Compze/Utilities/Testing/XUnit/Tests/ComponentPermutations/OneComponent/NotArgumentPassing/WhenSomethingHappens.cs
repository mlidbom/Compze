using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.OneComponent.NotArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;

   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");
   }

   [NotArgumentPassingOneComponentsPCT] public void ComponentsPermutationCurrentIsAvailableInConstructor() {}

   [NotArgumentPassingOneComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingOneComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}");
   }
}
