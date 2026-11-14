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

sealed class AValueTooManyMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingAValueTooMany",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class AValueTooManyAndAWildcardMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingAValueTooManyAndAWildcard",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class AValueTooFewMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingAValueTooFew",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class ANumberForAValueMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingANumberForAValue",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class ACommaSeparatedListForAValueMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingACommaSeparatedListForAValue",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class ADimensionTypeThatIsNotAnEnumMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute(
      configurationFileName: null,
      dimensionEnumTypes: [typeof(Serializer), typeof(string)],
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class NoDimensionsMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute(
      configurationFileName: null,
      dimensionEnumTypes: [],
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class SerializerAndSqlLayerFromASharedFileMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer>(
      configurationFileName: "TestUsingMicrosoftWithEveryValueOfTheSecondDimension",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

sealed class SerializerAndDIContainerFromASharedFileMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, DIContainer>(
      configurationFileName: "TestUsingMicrosoftWithEveryValueOfTheSecondDimension",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
