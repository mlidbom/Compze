using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using Xunit;

#pragma warning disable xUnit1026

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._5Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentCombination.Current.Serializer()}");
   }


   [ArgumentPassingFiveComponentsPCT] public void ComponentCombinationCurrentIsAvailableInConstructor(ComponentCombination combination) {}

   [ArgumentPassingFiveComponentsPCT] public void ThisIsTheCase(ComponentCombination combination) =>
      _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingFiveComponentsPCT] public void ThisIsAlsoTheCase(ComponentCombination combination) =>
         _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
   }
}
