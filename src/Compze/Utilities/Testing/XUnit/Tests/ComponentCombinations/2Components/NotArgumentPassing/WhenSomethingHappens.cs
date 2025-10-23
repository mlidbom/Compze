using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components.NotArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [NotArgumentPassingTwoComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}, SqlLayer enum: {ComponentsCombination.Current.SqlLayer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}, SqlLayer enum: {ComponentsCombination.Current.SqlLayer()}");
   }
}
