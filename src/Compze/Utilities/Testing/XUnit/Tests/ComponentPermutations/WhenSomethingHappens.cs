using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenSomethingHappens(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [OurPCT] public void ThisIsTheCase()
   {
      var current = ComponentsPermutation.Current!;

      // Components are strongly-typed enums - cast directly
      var serializer = (Serializer)current.Components[0];
      var sqlLayer = (SqlLayer)current.Components[1];

      _testOutputHelper.WriteLine($"✅ Serializer enum: {serializer}, SqlLayer enum: {sqlLayer}");
      _testOutputHelper.WriteLine($"✅ Component types: {current.Components[0].GetType().Name}, {current.Components[1].GetType().Name}");
   }

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [OurPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine(ComponentsPermutation.Current!.ToString()!);
   }
}
