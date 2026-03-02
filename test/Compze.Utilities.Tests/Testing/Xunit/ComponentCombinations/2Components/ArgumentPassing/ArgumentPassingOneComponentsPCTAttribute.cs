using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.ArgumentPassing;

sealed class ArgumentPassingTwoComponentsPCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingArgumentPassingTwoComponentsPCTAttribute",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
