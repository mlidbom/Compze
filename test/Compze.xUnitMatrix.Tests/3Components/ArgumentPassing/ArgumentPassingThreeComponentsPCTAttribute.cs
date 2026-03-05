using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._3Components.ArgumentPassing;

sealed class ArgumentPassingThreeComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingArgumentPassingThreeComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
