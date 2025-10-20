using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine(ComponentContext.CurrentPermutation!.ToString()!);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [PCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentContext.CurrentPermutation!.ToString()!);
   }
}
