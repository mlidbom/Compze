using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._1Component;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;

   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {MatrixCombination.Current.Serializer()}");
   }

   [OneComponentMatrix] public void MatrixCombinationCurrentIsAvailableInConstructor() {}

   [OneComponentMatrix] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {MatrixCombination.Current.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [OneComponentMatrix] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {MatrixCombination.Current.Serializer()}");
   }
}
