using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._5Components.ArgumentPassing;

sealed class ArgumentPassingFiveComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer, DIContainer, TeventStore, TessageBus>(
      configurationFileName: "TestUsingArgumentPassingFiveComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
