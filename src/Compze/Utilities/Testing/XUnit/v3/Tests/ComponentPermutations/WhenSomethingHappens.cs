using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [PCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);

   [PCT(Exclude = ["Type1Component1"])] public void ExcludedComponentType1Component1DoesNotExecute() =>
      _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [PCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);
   }
}
