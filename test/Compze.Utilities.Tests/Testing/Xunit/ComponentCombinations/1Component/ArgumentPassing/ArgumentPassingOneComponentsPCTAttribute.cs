using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._1Component.ArgumentPassing;

sealed class ArgumentPassingOneComponentPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer>(
      configurationFileName: "TestUsingArgumentPassingOneComponentsPCTAttribute",
      useTestMethodArgument:true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
