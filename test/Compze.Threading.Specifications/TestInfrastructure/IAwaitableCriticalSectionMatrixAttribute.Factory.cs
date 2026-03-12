using Compze.Threading.Interprocess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableCriticalSectionMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      // ReSharper disable once StaticMemberInGenericType
      static long _counter;
      readonly List<IDisposable> _disposables = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public IAwaitableCriticalSection Create(WaitTimeout waitTimeout) => Create(null, waitTimeout);
      public IAwaitableCriticalSection Create(LockTimeout lockTimeout) => Create(lockTimeout, null);

      public IAwaitableCriticalSection Create(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor              => IAwaitableMonitor.New(lockTimeout, waitTimeout),
            Implementation.GlobalPollingMutex   => UniqueName()
                                                  ._(it => IPollingAwaitableMutex.Global(it, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10)))
                                                  ._tap(_disposables.Add),
            Implementation.LocalPollingMutex    => UniqueName()
                                                  ._(it => IPollingAwaitableMutex.Local(it, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10)))
                                                  ._tap(_disposables.Add),
            Implementation.GlobalSignalingMutex => UniqueName()
                                                  ._(it => ISignalingAwaitableMutex.Global(it, lockTimeout, waitTimeout))
                                                  ._tap(_disposables.Add),
            Implementation.LocalSignalingMutex  => UniqueName()
                                                  ._(it => ISignalingAwaitableMutex.Local(it, lockTimeout, waitTimeout))
                                                  ._tap(_disposables.Add),
            _ => throw new ArgumentOutOfRangeException()
         };
      }

      static string UniqueName() => $"{typeof(TTest).FullName}.P{Environment.ProcessId}.{Interlocked.Increment(ref _counter)}";

      public void Dispose()
      {
         foreach(var disposable in _disposables)
            disposable.Dispose();
      }
   }
}
