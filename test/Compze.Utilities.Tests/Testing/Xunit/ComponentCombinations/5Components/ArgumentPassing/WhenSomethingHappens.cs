using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._5Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;

   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      PrintSerializer(ComponentCombination.Current);
   }

   [ArgumentPassingFiveComponentsPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) =>
      PrintSerializer(combination);

   [ArgumentPassingFiveComponentsPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      PrintSerializer(combination);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingFiveComponentsPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         PrintSerializer(combination);
   }

   void PrintSerializer(ComponentCombination combination) => _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
}
