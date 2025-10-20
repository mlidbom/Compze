using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit.Abstractions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class ComponentPermutationsTest(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void RunsWithEachComponentCombination() =>
      _testOutputHelper.WriteLine(TestContext.CurrentTestCase!.TestMethodArguments[0].ToString());
}
