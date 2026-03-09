using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._1Component.ArgumentPassing;

sealed class ArgumentPassingOneComponentPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingArgumentPassingOneComponentsPCTAttribute",
      useTestMethodArgument:true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
