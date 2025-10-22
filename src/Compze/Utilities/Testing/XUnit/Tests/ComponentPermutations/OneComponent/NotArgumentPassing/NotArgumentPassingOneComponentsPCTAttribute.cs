using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.OneComponent.NotArgumentPassing;

public sealed class NotArgumentPassingOneComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingNotArgumentPassingOneComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
