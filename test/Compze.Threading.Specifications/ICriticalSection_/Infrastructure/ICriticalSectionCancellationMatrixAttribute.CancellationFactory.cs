using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class ICriticalSectionCancellationMatrixAttribute
{
   public class CancellationFactory<TTest> : IDisposable
   {
      readonly ICriticalSectionMatrixAttribute.Factory<TTest> _innerFactory = new();

      // ReSharper disable once MemberCanBeMadeStatic.Local
      public CancellationMechanism CurrentCancellationMechanism => (CancellationMechanism)MatrixCombination.Current.DimensionValues[1];

      public Type ExpectedExceptionType => CurrentCancellationMechanism switch
      {
         CancellationMechanism.ThreadInterrupt  => typeof(ThreadInterruptedException),
         CancellationMechanism.CancellationToken => typeof(OperationCanceledException),
         _                                       => throw new ArgumentOutOfRangeException()
      };

      public ICriticalSection Create(LockTimeout? timeout = null) => _innerFactory.Create(timeout);

      public CancellationTrigger CreateCancellationTrigger() => CurrentCancellationMechanism switch
      {
         CancellationMechanism.ThreadInterrupt  => new ThreadInterruptCancellationTrigger(),
         CancellationMechanism.CancellationToken => new CancellationTokenCancellationTrigger(),
         _                                       => throw new ArgumentOutOfRangeException()
      };

      public void Dispose() => _innerFactory.Dispose();
   }
}
