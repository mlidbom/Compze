using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAllPermutationsAreMarkedAsExcluded
{
   public WhenAllPermutationsAreMarkedAsExcluded() => throw new Exception("Should not be executed");

   [TypedPCT(
      skippedComponents: [Serializer.Microsoft, Serializer.Newtonsoft],
      skipReasons: ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      [PCT(Skipped = ["invalid::TODO"])]
      public void NoTestsAreExecuted_() => throw new Exception("Should not be executed");
   }
}
