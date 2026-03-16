using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;

#pragma warning disable CA1813 // Avoid unsealed attributes — partial classes cannot use sealed keyword
partial class IAwaitableCriticalSectionCancellationMatrixAttribute
{
   public class CancellationFactory<TTest> : IDisposable
   {
      readonly IAwaitableCriticalSectionMatrixAttribute.Factory<TTest> _innerFactory = new();

      // ReSharper disable once MemberCanBeMadeStatic.Local
      public CancellationMechanism CurrentCancellationMechanism => (CancellationMechanism)MatrixCombination.Current.Components[1];

      public Type ExpectedExceptionType => CurrentCancellationMechanism switch
      {
         CancellationMechanism.ThreadInterrupt  => typeof(ThreadInterruptedException),
         CancellationMechanism.CancellationToken => typeof(OperationCanceledException),
         _                                       => throw new ArgumentOutOfRangeException()
      };

      public IAwaitableCriticalSection Create(WaitTimeout waitTimeout) => _innerFactory.Create(waitTimeout);
      public IAwaitableCriticalSection Create(LockTimeout lockTimeout) => _innerFactory.Create(lockTimeout);
      public IAwaitableCriticalSection Create(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => _innerFactory.Create(lockTimeout, waitTimeout);

      public CancellationTrigger CreateCancellationTrigger() => CurrentCancellationMechanism switch
      {
         CancellationMechanism.ThreadInterrupt  => new ThreadInterruptCancellationTrigger(),
         CancellationMechanism.CancellationToken => new CancellationTokenCancellationTrigger(),
         _                                       => throw new ArgumentOutOfRangeException()
      };

      public void Dispose() => _innerFactory.Dispose();
   }
}
