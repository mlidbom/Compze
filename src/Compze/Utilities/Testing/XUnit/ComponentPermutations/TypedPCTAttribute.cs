using System.Runtime.CompilerServices;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Type-safe version of PCT attribute for tests.
/// Uses Type1Component and Type2Component enums instead of strings.
/// NOTE: This is in the main library (not Tests project) because XUnit test discovery
/// requires the discoverer attribute and the concrete attribute class to be in the same assembly.
/// </summary>
/// <example>
/// [TypedPCT(
///    skippedComponents: [Type1Component.Type1Component1, Type2Component.Type2Component3],
///    skipReasons: ["Not implemented yet", "Deprecated"])]
/// public void MyTest() { }
/// </example>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public sealed class TypedPCTAttribute(
   object[]? skippedComponents = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : TypedPluggableComponentsTheoryAttribute<Type1Component, Type2Component>(
      skippedComponents, skipReasons, sourceFilePath, sourceLineNumber)
{
}
