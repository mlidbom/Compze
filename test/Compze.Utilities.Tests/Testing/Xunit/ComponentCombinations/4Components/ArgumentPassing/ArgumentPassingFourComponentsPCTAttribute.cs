using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._4Components.ArgumentPassing;

sealed class ArgumentPassingFourComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer, TeventStore>(
      configurationFileName: "TestUsingArgumentPassingFourComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
