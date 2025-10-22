using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

/// <summary>
/// Type-safe version of PCT attribute for tests.
/// Uses Serializer and SqlLayer enums instead of strings.
/// </summary>
/// <example>
/// [TypedPCT(
///    skippedComponents: [Serializer.Microsoft, SqlLayer.MySql],
///    skipReasons: ["Not implemented yet", "Deprecated"])]
/// public void MyTest() { }
/// </example>
[XunitTestCaseDiscoverer(typeof(TypedPluggableComponentsTheoryDiscoverer))]
public sealed class TypedPCTAttribute(
   object[]? skippedComponents = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : TypedPluggableComponentsTheoryAttribute<Serializer, SqlLayer>(
      skippedComponents?.OfType<Enum>().ToList(),
      skipReasons,
      sourceFilePath,
      sourceLineNumber);


public class TypedPluggableComponentsTheoryDiscoverer : PluggableComponentsTheoryDiscoverer
{
}
