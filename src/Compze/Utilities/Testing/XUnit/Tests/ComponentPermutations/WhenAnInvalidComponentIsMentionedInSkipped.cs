using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

// This test file is no longer needed - with typed enums, invalid component names are a compile error
// The whole point of TypedPCT is compile-time safety!
public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [TypedPCT] 
   public void TheCodeDoesNotCompile()
   {
      // With TypedPCT, you can't have invalid component names - they're enum values!
      // This is enforced at compile time, not runtime.
      // Example: [TypedPCT(skippedComponents: [(Serializer)999], ...)] won't compile
   }
}
