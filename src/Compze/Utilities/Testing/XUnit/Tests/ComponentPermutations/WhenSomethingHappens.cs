using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [TypedPCT] public void ThisIsTheCase()
   {
      var current = ComponentsPermutation.Current!;
      
      // Access as strongly-typed enums
      var serializer = (Serializer)current.ComponentEnums[0];
      var sqlLayer = (SqlLayer)current.ComponentEnums[1];
      
      _testOutputHelper.WriteLine($"Serializer: {serializer}, SqlLayer: {sqlLayer}");
   }

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [TypedPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);
   }
}
