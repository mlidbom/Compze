using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix.Tests.Configuration;

sealed class MissingConfigurationFileMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingAMissingConfigurationFile",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class InvalidDimensionValueMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingAnInvalidDimensionValue",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class EmptyConfigurationFileMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingNoCombinations",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
