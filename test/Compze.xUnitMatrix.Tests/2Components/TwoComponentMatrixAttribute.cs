using System.Runtime.CompilerServices;

namespace Compze.xUnitMatrix.Tests._2Components;

sealed class TwoComponentMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingTwoComponentMatrix",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
