using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.OneComponent.ArgumentPassing;

public sealed class ArgumentPassingOneComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingArgumentPassingOneComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
