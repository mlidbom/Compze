using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit.Abstractions;

namespace Compze.Utilities.Testing.XUnit.v2.Tests.ComponentPermutations;

public class ComponentPermutationsTest(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void RunsWithEachComponentCombination(string _) =>
      _testOutputHelper.WriteLine(TestContext.CurrentTestCase!.TestMethodArguments[0].ToString());

   public class Inner(ITestOutputHelper testOutputHelper) : ComponentPermutationsTest(testOutputHelper)
   {
      [PCT] public void AlsoRunsWithEachComponentCombination(string _) =>
         _testOutputHelper.WriteLine(TestContext.CurrentTestCase!.TestMethodArguments[0].ToString());
   }
}
