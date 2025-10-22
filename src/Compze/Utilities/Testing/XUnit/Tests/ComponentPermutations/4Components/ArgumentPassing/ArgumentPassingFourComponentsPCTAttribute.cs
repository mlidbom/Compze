using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._4Components.ArgumentPassing;

public sealed class ArgumentPassingFourComponentsPCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute<Serializer, SqlLayer, DIContainer, EventStore>(
      configurationFileName: "TestUsingArgumentPassingFourComponentsPCTAttribute",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
