using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;
using Xunit;

#pragma warning disable xUnit1026

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._5Components.ArgumentPassing;

public class WhenSomethingHappens
{
   readonly ITestOutputHelper _testOutputHelper;
   public WhenSomethingHappens(ITestOutputHelper testOutputHelper)
   {
      _testOutputHelper = testOutputHelper;
      _testOutputHelper.WriteLine($"Serializer enum: {ComponentsCombination.Current.Serializer()}");
   }


   [ArgumentPassingFiveComponentsPCTAttribute] public void ComponentsCombinationCurrentIsAvailableInConstructor(ComponentsCombination combination) {}

   [ArgumentPassingFiveComponentsPCTAttribute] public void ThisIsTheCase(ComponentsCombination combination) =>
      _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");

   public class AndSomethingElseHappens(ITestOutputHelper testOutputHelper) : WhenSomethingHappens(testOutputHelper)
   {
      [ArgumentPassingFiveComponentsPCTAttribute] public void ThisIsAlsoTheCase(ComponentsCombination combination) =>
         _testOutputHelper.WriteLine($"Serializer enum: {combination.Serializer()}");
   }
}
