using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._5Components.ArgumentPassing;

sealed class ArgumentPassingFiveComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer, TeventStore, TessageBus>(
      configurationFileName: "TestUsingArgumentPassingFiveComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
