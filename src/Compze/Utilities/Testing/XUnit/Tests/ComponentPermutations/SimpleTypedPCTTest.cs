using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class SimpleTypedPCTTest
{
   [TypedPCT]
   public void SimpleTest()
   {
      // Just a simple test to see if TypedPCT discovery works
      var current = ComponentsPermutation.Current;
      // Test passes if we get here
   }

   [TypedPCT(skippedComponents: [Type1Component.Type1Component1], skipReasons: ["Test skip"])]
   public void TestWithSkip()
   {
      var current = ComponentsPermutation.Current;
      // Should not be called for Type1Component1
   }
}
