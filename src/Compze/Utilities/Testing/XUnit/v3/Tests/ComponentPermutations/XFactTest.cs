using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class ComponentPermutationsTest(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void RunsWithEachComponentCombination() =>
      _testOutputHelper.WriteLine(((PluggableComponentsTestCase)TestContext.CurrentTestCase!).TestMethodArguments.ToString()!);

   public class Inner(ITestOutputHelper testOutputHelper) : ComponentPermutationsTest(testOutputHelper)
   {
      [PCT] public void AlsoRunsWithEachComponentCombination() =>
         _testOutputHelper.WriteLine(((PluggableComponentsTestCase)TestContext.CurrentTestCase!).TestMethodArguments.ToString()!);
   }
}
