using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class ComponentPermutationsTest(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void RunsWithEachComponentCombination()
   {
      var context = TestContext.Current;
   }

   public class Inner(ITestOutputHelper testOutputHelper) : ComponentPermutationsTest(testOutputHelper)
   {
      [PCT] public void AlsoRunsWithEachComponentCombination()
      {
         var context = TestContext.Current;
      }
   }
}
