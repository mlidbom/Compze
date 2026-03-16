using System.Runtime.CompilerServices;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class ICriticalSectionCancellationMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<ICriticalSectionMatrixAttribute.Implementation, CancellationMechanism>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
