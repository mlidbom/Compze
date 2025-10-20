using Compze.Utilities.Testing.XUnit.v2.ComponentPermutations;
using Xunit.Abstractions;
#pragma warning disable xUnit1003 //we may be using a theory attribute, but it does not require manually passing data

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
