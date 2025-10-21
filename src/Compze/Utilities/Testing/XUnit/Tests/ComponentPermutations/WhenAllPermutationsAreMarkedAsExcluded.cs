using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenAllPermutationsAreMarkedAsExcluded
{
   public WhenAllPermutationsAreMarkedAsExcluded() => throw new Exception("Should not be executed");

   [PCT(Exclude = ["Type1Component1", "Type1Component2"])] public void NoTestsAreExecuted() => throw new Exception("Should not be executed");

   public class NestedScenarioWithReasons
   {
      public NestedScenarioWithReasons() => throw new Exception("Should not be executed");

      [PCT(Exclude = ["Type1Component1::Not implemented yet", "Type1Component2::Known to fail"])]
      public void NoTestsAreExecuted_WithReasons() => throw new Exception("Should not be executed");
   }

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", "Constructor should not run for the excluded component");

      [PCT(Exclude = ["Type1Component1", "Type1Component2"])] public void NoTestsAreExecuted_() => throw new Exception("Should not be executed");
   }
}
