using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._3Components.ArgumentPassing;

sealed class ArgumentPassingThreeComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingArgumentPassingThreeComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
