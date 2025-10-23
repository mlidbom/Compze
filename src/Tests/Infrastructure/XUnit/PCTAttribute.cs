using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Wiring.Testing;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tests.Infrastructure.XUnit;

public sealed class PCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<SqlLayer, DIContainer>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
}
