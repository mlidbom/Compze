using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.TestInfrastructure;

sealed class IAwaitableCriticalSectionMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<AwaitableCriticalSectionImplementation>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
