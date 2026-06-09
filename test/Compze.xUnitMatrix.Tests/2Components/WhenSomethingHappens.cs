namespace Compze.xUnitMatrix.Tests._2Components;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [TwoComponentMatrix] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {MatrixCombination.Current.Serializer()}, SqlLayer enum: {MatrixCombination.Current.SqlLayer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [TwoComponentMatrix] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {MatrixCombination.Current.Serializer()}, SqlLayer enum: {MatrixCombination.Current.SqlLayer()}");
   }
}
