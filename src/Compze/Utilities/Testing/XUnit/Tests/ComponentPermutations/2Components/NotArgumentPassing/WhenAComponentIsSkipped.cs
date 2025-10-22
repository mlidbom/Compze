using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components.NotArgumentPassing;

public class WhenAComponentIsSkipped
{
   public WhenAComponentIsSkipped() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

   [NotArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft);

   public class InANestedScenario
   {
      public InANestedScenario() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

      [NotArgumentPassingTwoComponentsPCT(skipped: [Serializer.Microsoft], skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentsPermutation.Current.Serializer().Should().NotBe(Serializer.Microsoft);
   }
}
