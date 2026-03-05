using Compze.xUnitMatrix;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._1Component.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      PrintSerializer(ComponentCombination.Current);
   }


   [ArgumentPassingOneComponentPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) =>
      PrintSerializer(combination);

   [ArgumentPassingOneComponentPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      PrintSerializer(combination);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingOneComponentPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         PrintSerializer(combination);
   }

   void PrintSerializer(ComponentCombination combination) => _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
}
