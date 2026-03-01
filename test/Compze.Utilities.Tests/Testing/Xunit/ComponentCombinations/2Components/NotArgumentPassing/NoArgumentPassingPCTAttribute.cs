using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.NotArgumentPassing;

internal sealed class NotArgumentPassingTwoComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNotArgumentPassingTwoComponentsPCTAttribute",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
