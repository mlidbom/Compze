using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenAComponentIsSkipped
{
   public WhenAComponentIsSkipped() => ComponentCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

   [NotArgumentPassingTwoComponentsPCT(Skipped = [Serializer.Microsoft], SkipReasons = ["TODO"])]
   public void TestIsNotExecutedForThatComponent() => ComponentCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft);

   public class InANestedScenario
   {
      public InANestedScenario() => ComponentCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft, "Constructor should not run for the excluded component");

      [NotArgumentPassingTwoComponentsPCT(Skipped = [Serializer.Microsoft], SkipReasons = ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent() => ComponentCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft);
   }
}
