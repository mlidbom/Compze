using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentCombination.Current.Serializer()}");
   }


   [ArgumentPassingTwoComponentsPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) {}

   [ArgumentPassingTwoComponentsPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
   }
}
