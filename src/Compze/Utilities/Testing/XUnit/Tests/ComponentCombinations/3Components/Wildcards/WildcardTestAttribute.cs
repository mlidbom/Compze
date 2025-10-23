using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._3Components.Wildcards;

public sealed class WildcardTestAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentsCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingWildcards",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
