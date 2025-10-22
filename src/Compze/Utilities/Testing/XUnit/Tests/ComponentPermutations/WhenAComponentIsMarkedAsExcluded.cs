using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAComponentIsMarkedAsExcluded
{
   public WhenAComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Component1", "Constructor should not run for the excluded component");

   [TypedPCT(skippedComponents: [ComponentType1.Component1], skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Component1");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Component1", "Constructor should not run for the excluded component");

      [TypedPCT(skippedComponents: [ComponentType1.Component1], skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Component1");
   }
}
