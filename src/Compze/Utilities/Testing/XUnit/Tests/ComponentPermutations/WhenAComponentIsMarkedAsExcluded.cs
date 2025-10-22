using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAComponentIsMarkedAsExcluded
{
   public WhenAComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Microsoft", "Constructor should not run for the excluded component");

   [TypedPCT(skipped: [Serializer.Microsoft], skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Microsoft");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Microsoft", "Constructor should not run for the excluded component");

      [TypedPCT(skipped: [Serializer.Microsoft], skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Microsoft");
   }
}
