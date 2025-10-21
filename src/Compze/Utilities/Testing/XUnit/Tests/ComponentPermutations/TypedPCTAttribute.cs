using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

/// <summary>
/// Type-safe version of PCT attribute specifically for these tests.
/// Uses Type1Component and Type2Component enums instead of strings.
/// </summary>
/// <example>
/// [TypedPCT(
///    skippedComponents1: [Type1Component.Type1Component1],
///    skipReasons1: ["Not implemented yet"])]
/// public void MyTest() { }
/// </example>
public sealed class TypedPCTAttribute(
   object[]? skippedComponents1 = null,
   string[]? skipReasons1 = null,
   object[]? skippedComponents2 = null,
   string[]? skipReasons2 = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : TypedPluggableComponentsTheoryAttribute<Type1Component, Type2Component>(
      skippedComponents1, skipReasons1, skippedComponents2, skipReasons2, sourceFilePath, sourceLineNumber)
{
}
