using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NoArgumentPassing;

public class WhenAComponentIsSkipped
{
   public WhenAComponentIsSkipped() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

   [NoArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft);

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

      [NoArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft);
   }
}
