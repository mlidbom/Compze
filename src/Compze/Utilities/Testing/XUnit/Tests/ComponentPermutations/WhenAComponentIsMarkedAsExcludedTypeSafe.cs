using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

/// <summary>
/// Demonstrates the new type-safe API for excluding components.
/// Compare with WhenAComponentIsMarkedAsExcluded.cs which uses the string-based API.
/// </summary>
public class WhenAComponentIsMarkedAsExcludedTypeSafe
{
   public WhenAComponentIsMarkedAsExcludedTypeSafe() => 
      ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", 
         "Constructor should not run for the excluded component");

   /// <summary>
   /// Type-safe version: no string literals, compile-time checking, IntelliSense support
   /// </summary>
   [TypedPCT(
      skippedComponents: [Type1Component.Type1Component1],
      skipReasons: ["TODO"])]
   public void TestIsNotExecutedForThatComponent_TypeSafe() => 
      ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1");

   /// <summary>
   /// You can skip multiple components from any dimension with reasons.
   /// Much cleaner than having separate arrays per dimension!
   /// </summary>
   [TypedPCT(
      skippedComponents: [
         Type1Component.Type1Component1, 
         Type1Component.Type1Component3,
         Type2Component.Type2Component3
      ],
      skipReasons: [
         "Not implemented yet", 
         "Deprecated",
         "Unsupported configuration"
      ]
   )]
   public void TestWithMultipleExclusions() => 
      ComponentsPermutation.Current!.Components[0].Should().BeOneOf("Type1Component2");

   public class NestedScenarioComponentIsMarkedAsExcluded
   {
      public NestedScenarioComponentIsMarkedAsExcluded() => 
         ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1", 
            "Constructor should not run for the excluded component");

      [TypedPCT(
         skippedComponents: [Type1Component.Type1Component1],
         skipReasons: ["Unsupported"])]
      public void TestIsNotExecutedForThatComponent_TypeSafe() => 
         ComponentsPermutation.Current!.Components[0].Should().NotBe("Type1Component1");
   }
}
