using Compze.Threading.Interprocess;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.ResourceAccess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableSharedMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      // ReSharper disable once StaticMemberInGenericType
      static long _counter;
      readonly List<IDisposable> _disposables = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public IAwaitableShared<TShared> Create<TShared>(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor              => IAwaitableThreadShared.New(shared, lockTimeout, waitTimeout),
            Implementation.GlobalPollingMutex   => IAwaitableProcessShared.New(shared, UniqueName()
                                                  ._(it => IPollingAwaitableMutex.Global(it, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10)))
                                                  ._tap(_disposables.Add)),
            Implementation.LocalPollingMutex    => IAwaitableProcessShared.New(shared, UniqueName()
                                                  ._(it => IPollingAwaitableMutex.Local(it, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10)))
                                                  ._tap(_disposables.Add)),
            Implementation.GlobalSignalingMutex => IAwaitableProcessShared.New(shared, UniqueName()
                                                  ._(it => ISignalingAwaitableMutex.Global(it, lockTimeout, waitTimeout))
                                                  ._tap(_disposables.Add)),
            Implementation.LocalSignalingMutex  => IAwaitableProcessShared.New(shared, UniqueName()
                                                  ._(it => ISignalingAwaitableMutex.Local(it, lockTimeout, waitTimeout))
                                                  ._tap(_disposables.Add)),
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
