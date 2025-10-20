using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit.Abstractions;

namespace Compze.Utilities.Testing.XUnit.v2.Tests.ComponentPermutations;

public class ComponentPermutationsTest(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void RunsWithEachComponentCombination() =>
      _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString());

   public class Inner(ITestOutputHelper testOutputHelper) : ComponentPermutationsTest(testOutputHelper)
   {
      [PCT] public void AlsoRunsWithEachComponentCombination() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString());
   }
}
