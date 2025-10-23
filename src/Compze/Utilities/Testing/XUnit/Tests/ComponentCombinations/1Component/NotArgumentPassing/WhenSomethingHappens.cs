using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._1Component.NotArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;

   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}");
   }

   [NotArgumentPassingOneComponentsPCT] public void ComponentsCombinationCurrentIsAvailableInConstructor() {}

   [NotArgumentPassingOneComponentsPCT] public void ThisIsTheCase() =>
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [NotArgumentPassingOneComponentsPCT] public void ThisIsAlsoTheCase() =>
         _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}");
   }
}
