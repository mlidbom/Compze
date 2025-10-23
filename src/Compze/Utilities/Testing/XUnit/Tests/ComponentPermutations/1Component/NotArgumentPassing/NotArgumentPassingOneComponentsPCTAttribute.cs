using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentsPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._1Component.NotArgumentPassing;

public sealed class NotArgumentPassingOneComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentsPermutationsTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingNotArgumentPassingOneComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
