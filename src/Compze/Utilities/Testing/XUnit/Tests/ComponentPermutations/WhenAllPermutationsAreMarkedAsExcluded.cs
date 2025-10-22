using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class WhenAllPermutationsAreMarkedAsExcluded
{
   public WhenAllPermutationsAreMarkedAsExcluded() => throw new Exception("Should not be executed");

   [TypedPCT(
      skippedComponents: [Type1Component.Type1Component1, Type1Component.Type1Component2],
      skipReasons: ["TODO", "Not supported"])]
   public void NoTestsAreExecuted() => throw new Exception("Should not be executed");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      [PCT(Skipped = ["invalid::TODO"])]
      public void NoTestsAreExecuted_() => throw new Exception("Should not be executed");
   }
}
