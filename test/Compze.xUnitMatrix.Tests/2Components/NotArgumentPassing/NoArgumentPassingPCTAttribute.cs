using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

namespace Compze.xUnitMatrix.Tests._2Components.NotArgumentPassing;

sealed class NotArgumentPassingTwoComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNotArgumentPassingTwoComponentsPCTAttribute",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
