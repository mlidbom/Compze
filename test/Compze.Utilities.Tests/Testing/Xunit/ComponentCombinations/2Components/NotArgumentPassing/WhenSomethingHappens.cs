using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [NotArgumentPassingTwoComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentCombination.Current.Serializer()}, SqlLayer enum: {ComponentCombination.Current.SqlLayer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentCombination.Current.Serializer()}, SqlLayer enum: {ComponentCombination.Current.SqlLayer()}");
   }
}
