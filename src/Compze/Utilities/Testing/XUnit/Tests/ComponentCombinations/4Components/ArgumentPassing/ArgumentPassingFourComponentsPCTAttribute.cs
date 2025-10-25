using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._4Components.ArgumentPassing;

public sealed class ArgumentPassingFourComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer, TeventStore>(
      configurationFileName: "TestUsingArgumentPassingFourComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
