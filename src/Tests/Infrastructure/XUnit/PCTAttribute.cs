using System.Runtime.CompilerServices;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Tests.Infrastructure.XUnit;

public sealed class PCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<SqlLayer, DIContainer, Serializer>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
}
