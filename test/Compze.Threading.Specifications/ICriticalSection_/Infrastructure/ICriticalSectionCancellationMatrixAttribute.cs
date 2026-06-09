using System.Runtime.CompilerServices;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class ICriticalSectionCancellationMatrixAttribute
   : MatrixTheoryAttribute<ICriticalSectionMatrixAttribute.Implementation, CancellationMechanism>
{
   public ICriticalSectionCancellationMatrixAttribute(
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base(
         configurationFileName: null,
         sourceFilePath: sourceFilePath,
         sourceLineNumber: sourceLineNumber) { }
}
