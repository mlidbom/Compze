using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.v3.Tests.ComponentPermutations;

public class WhenAComponentIsMarkedAsExcluded
{
   public WhenAComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", "Constructor should not run for the excluded component");

   [PCT(Exclude = ["Type1Component1"])] public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", "Constructor should not run for the excluded component");

      [PCT(Exclude = ["Type1Component1"])] public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1");
   }
}
