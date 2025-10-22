using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [TypedPCT] public void ThisIsTheCase()
   {
      var current = ComponentsPermutation.Current!;
      _testOutputHelper.WriteLine($"String: {current}");
      _testOutputHelper.WriteLine($"Component[0] Type: {current.Components[0].GetType().Name} Value: {current.Components[0]}");
      _testOutputHelper.WriteLine($"Component[1] Type: {current.Components[1].GetType().Name} Value: {current.Components[1]}");
   }

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [TypedPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);
   }
}
