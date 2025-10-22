using System.Runtime.CompilerServices;
using Xunit.v3;
// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Type-safe version of PluggableComponentsTheoryAttribute with two component dimensions.
/// Instead of using string arrays for skipped components, use strongly-typed enum values in constructor.
/// Due to C# attribute limitations, we use object arrays that must contain enum values.
/// </summary>
/// <typeparam name="TComponent1">First component dimension enum type</typeparam>
/// <typeparam name="TComponent2">Second component dimension enum type</typeparam>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public abstract class TypedPluggableComponentsTheoryAttribute<TComponent1, TComponent2> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
{
   /// <summary>
   /// Creates a type-safe pluggable components theory attribute.
   /// </summary>
   /// <param name="skippedComponents">Array of enum values to skip (can be any of TComponent1 or TComponent2)</param>
   /// <param name="skipReasons">Corresponding reasons for skipping (must match length of skippedComponents)</param>
   protected TypedPluggableComponentsTheoryAttribute(
      object[]? skippedComponents = null,
      string[]? skipReasons = null,
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base([typeof(TComponent1), typeof(TComponent2)], sourceFilePath, sourceLineNumber)
   {
      InitializeTypedSkipped(skippedComponents, skipReasons);
   }
}
