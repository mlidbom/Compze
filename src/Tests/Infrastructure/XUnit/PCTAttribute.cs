using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentsPermutations;
using Compze.Wiring.Testing;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tests.Infrastructure.XUnit;

public sealed class PCTAttribute(
   object[]? skipped = null,
   string[]? skipReasons = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentsPermutationsTheoryAttribute<SqlLayer, DIContainer>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      skipped: skipped,
      skipReasons: skipReasons,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
