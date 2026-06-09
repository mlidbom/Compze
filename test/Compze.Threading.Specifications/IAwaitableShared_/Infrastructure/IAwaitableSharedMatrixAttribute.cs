using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class IAwaitableSharedMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<IAwaitableSharedMatrixAttribute.Implementation>(
      configurationFileName: null,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
