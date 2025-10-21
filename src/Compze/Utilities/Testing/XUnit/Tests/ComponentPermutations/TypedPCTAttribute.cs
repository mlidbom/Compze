using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

/// <summary>
/// Type-safe version of PCT attribute specifically for these tests.
/// Uses Type1Component and Type2Component enums instead of strings.
/// </summary>
/// <example>
/// [TypedPCT(
///    skippedComponents: [Type1Component.Type1Component1, Type2Component.Type2Component3],
///    skipReasons: ["Not implemented yet", "Deprecated"])]
/// public void MyTest() { }
/// </example>
public sealed class TypedPCTAttribute(
   object[]? skippedComponents = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : TypedPluggableComponentsTheoryAttribute<Type1Component, Type2Component>(
      skippedComponents, skipReasons, sourceFilePath, sourceLineNumber)
{
}
