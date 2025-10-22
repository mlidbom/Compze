using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [TypedPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [TypedPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);
   }
}
