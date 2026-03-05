using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

sealed class WildcardTestAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingWildcards",
      useTestMethodArgument: true,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
