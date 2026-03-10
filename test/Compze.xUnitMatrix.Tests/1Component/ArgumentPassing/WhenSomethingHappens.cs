using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._1Component.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      PrintSerializer(MatrixCombination.Current);
   }


   [ArgumentPassingOneComponentPCT] public void MatrixCombinationCurrentIsAvailableInConstructor(MatrixCombination combination) =>
      PrintSerializer(combination);

   [ArgumentPassingOneComponentPCT] public void ThisIsTheCase(MatrixCombination combination) =>
      PrintSerializer(combination);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingOneComponentPCT] public void ThisIsAlsoTheCase(MatrixCombination combination) =>
         PrintSerializer(combination);
   }

   void PrintSerializer(MatrixCombination combination) => _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
}
