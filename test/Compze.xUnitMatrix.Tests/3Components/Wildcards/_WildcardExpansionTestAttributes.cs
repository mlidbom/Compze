using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

sealed class MultipleWildcardsTestAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingMultipleWildcards",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class WildcardDeduplicationTestAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer, DIContainer>(
      configurationFileName: "TestUsingWildcardDeduplication",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
