using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations.TwoComponents.NotArgumentPassing;

public sealed class NotArgumentPassingTwoComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNotArgumentPassingTwoComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
