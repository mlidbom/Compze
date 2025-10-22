using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.ArgumentPassing;

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
public sealed class ArgumentPassingTwoComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingArgumentPassingTwoComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
