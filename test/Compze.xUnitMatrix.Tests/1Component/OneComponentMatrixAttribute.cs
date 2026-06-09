using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._1Component;

sealed class OneComponentMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingOneComponentMatrix",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
