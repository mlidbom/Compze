using System.Runtime.CompilerServices;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Type-safe version of PCT attribute for tests.
/// Uses Serializer and SqlLayer enums instead of strings.
/// NOTE: This is in the main library (not Tests project) because XUnit test discovery
/// requires the discoverer attribute and the concrete attribute class to be in the same assembly.
/// </summary>
/// <example>
/// [TypedPCT(
///    skippedComponents: [Serializer.Microsoft, SqlLayer.MySql],
///    skipReasons: ["Not implemented yet", "Deprecated"])]
/// public void MyTest() { }
/// </example>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public sealed class TypedPCTAttribute(
   object[]? skippedComponents = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : TypedPluggableComponentsTheoryAttribute<Serializer, SqlLayer>(
      skippedComponents, skipReasons, sourceFilePath, sourceLineNumber)
{
}
