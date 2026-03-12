using Compze.Threading.Interprocess;
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
            Implementation.Monitor => IAwaitableMonitor.New(lockTimeout, waitTimeout),
            Implementation.Mutex => CreatePollingAwaitableMutex(lockTimeout, waitTimeout),
            Implementation.SignalingMutex => CreateSignalingAwaitableMutex(lockTimeout, waitTimeout),
            _ => throw new ArgumentOutOfRangeException()
         };
      }

      IPollingAwaitableMutex CreatePollingAwaitableMutex(LockTimeout? lockTimeout, WaitTimeout? waitTimeout)
      {
         var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
         var mutex = IPollingAwaitableMutex.Global(uniqueName, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10));
         _disposables.Add(mutex);
         return mutex;
      }

      ISignalingAwaitableMutex CreateSignalingAwaitableMutex(LockTimeout? lockTimeout, WaitTimeout? waitTimeout)
      {
         var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
         var mutex = ISignalingAwaitableMutex.Global(uniqueName, lockTimeout, waitTimeout);
         _disposables.Add(mutex);
         return mutex;
      }

      public void Dispose()
      {
         foreach(var disposable in _disposables)
            disposable.Dispose();
      }
   }
}
