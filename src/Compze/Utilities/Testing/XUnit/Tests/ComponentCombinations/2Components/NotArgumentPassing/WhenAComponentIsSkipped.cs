using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenAComponentIsSkipped
{
   public WhenAComponentIsSkipped() => ComponentCombination.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

   [NotArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentCombination.Current.Serializer().Should().NotBe(Serializer.Microsoft);

   public class InANestedScenario
   {
      public InANestedScenario() => ComponentCombination.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

      [NotArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentCombination.Current.Serializer().Should().NotBe(Serializer.Microsoft);
   }
}
