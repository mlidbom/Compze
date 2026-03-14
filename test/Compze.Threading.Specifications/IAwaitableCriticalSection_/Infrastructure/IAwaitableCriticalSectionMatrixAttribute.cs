using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class IAwaitableCriticalSectionMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<IAwaitableCriticalSectionMatrixAttribute.Implementation>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
