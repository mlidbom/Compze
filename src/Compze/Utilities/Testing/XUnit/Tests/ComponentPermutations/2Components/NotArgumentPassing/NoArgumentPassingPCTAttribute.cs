using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentsPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components.NotArgumentPassing;

public sealed class NotArgumentPassingTwoComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentsPermutationsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNotArgumentPassingTwoComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
