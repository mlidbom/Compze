using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using Xunit;

#pragma warning disable xUnit1026

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._4Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}");
   }


   [ArgumentPassingFourComponentsPCT] public void ComponentsCombinationCurrentIsAvailableInConstructor(ComponentsCombination combination) {}

   [ArgumentPassingFourComponentsPCT] public void ThisIsTheCase(ComponentsCombination combination) =>
      _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingFourComponentsPCT] public void ThisIsAlsoTheCase(ComponentsCombination combination) =>
         _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
   }
}
