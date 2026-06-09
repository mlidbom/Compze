using System.Runtime.CompilerServices;

namespace Compze.xUnitMatrix.Tests._2Components.NotArgumentPassing;

sealed class NotArgumentPassingTwoComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNotArgumentPassingTwoComponentsPCTAttribute",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
