using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._1Component.NotArgumentPassing;

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
