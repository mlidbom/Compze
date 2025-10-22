using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NotArgumentPassing;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [NotArgumentPassingTwoComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}, SqlLayer enum: {ComponentsPermutation.Current.SqlLayer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingTwoComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentsPermutation.Current.Serializer()}, SqlLayer enum: {ComponentsPermutation.Current.SqlLayer()}");
   }
}
