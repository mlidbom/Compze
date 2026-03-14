using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.IShared_.IProcessShared_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class IProcessSharedMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<IProcessSharedMatrixAttribute.Implementation>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
