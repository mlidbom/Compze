using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._1Component.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentCombination.Current.Serializer()}");
   }


   [ArgumentPassingOneComponentPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) => _testOutputHelper.WriteLine(combination.ToString());

   [ArgumentPassingOneComponentPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingOneComponentPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
   }
}
