using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAllPermutationsAreMarkedAsExcluded
{
   public WhenAllPermutationsAreMarkedAsExcluded() => throw new Exception("Should not be executed");

   [PCT(Skipped = ["Type1Component1::TODO", "Type1Component2::Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", "Constructor should not run for the excluded component");

      [PCT(Skipped = ["Type1Component1::TODO", "Type1Component2::Not supported"])]
      public void NoTestsAreExecuted_() => throw new Exception("Should not be executed");
   }
}
