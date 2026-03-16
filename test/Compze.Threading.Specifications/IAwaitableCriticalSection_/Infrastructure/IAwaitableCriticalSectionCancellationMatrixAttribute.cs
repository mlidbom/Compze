using System.Runtime.CompilerServices;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class IAwaitableCriticalSectionCancellationMatrixAttribute
   : MatrixTheoryAttribute<IAwaitableCriticalSectionMatrixAttribute.Implementation, CancellationMechanism>
{
   // TODO: Remove SkipValues when CancellationToken parameter is added to IAwaitableCriticalSection (Phase 3)
   public IAwaitableCriticalSectionCancellationMatrixAttribute(
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base(
         configurationFileName: null,
         useTestMethodArgument: false,
         sourceFilePath: sourceFilePath,
         sourceLineNumber: sourceLineNumber)
      => SkipValues(CancellationMechanism.CancellationToken, "CancellationToken parameter not yet added to the interface");
}
