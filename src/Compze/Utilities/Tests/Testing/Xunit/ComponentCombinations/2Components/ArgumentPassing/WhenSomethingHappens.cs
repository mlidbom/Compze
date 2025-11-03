using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Xunit;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;

   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      PrintSerializer(ComponentCombination.Current);
   }

   [ArgumentPassingTwoComponentsPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) =>
      PrintSerializer(combination);

   [ArgumentPassingTwoComponentsPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      PrintSerializer(combination);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         PrintSerializer(combination);
   }

   void PrintSerializer(ComponentCombination combination) => _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
}
