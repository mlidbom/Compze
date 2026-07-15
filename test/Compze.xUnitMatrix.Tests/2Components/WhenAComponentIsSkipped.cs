

namespace Compze.xUnitMatrix.Tests._2Components;

public class WhenAComponentIsSkipped
{
   public WhenAComponentIsSkipped() => MatrixCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft, "Constructor should not run for the skipped component");

   [TwoComponentMatrix]
   [Skip<Serializer>(Serializer.Microsoft, "TODO")]
   public void TestIsNotExecutedForThatComponent() => MatrixCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft);

   public class InANestedScenario
   {
      public InANestedScenario() => MatrixCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft, "Constructor should not run for the skipped component");

      [TwoComponentMatrix]
      [Skip<Serializer>(Serializer.Microsoft, "Unsupported")]
      public void TestIsNotExecutedForThatComponent() => MatrixCombination.Current.Serializer().Must().NotBe(Serializer.Microsoft);
   }
}
