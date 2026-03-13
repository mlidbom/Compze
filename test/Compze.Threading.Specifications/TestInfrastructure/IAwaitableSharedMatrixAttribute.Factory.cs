using Compze.InterprocessObject;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.ResourceAccess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableSharedMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      readonly List<IDisposable> _disposables = [];
      readonly List<IInterprocessObject<SharedTestValue>> _interprocessObjects = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public IAwaitableShared<SharedTestValue> Create(SharedTestValue shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor => IAwaitableThreadShared.New(shared, lockTimeout, waitTimeout),
            Implementation.GlobalPollingMutex => IAwaitableProcessShared.GlobalPolling(UniqueName(), shared, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10))
                                                                        ._tap(_disposables.Add),
            Implementation.LocalPollingMutex => IAwaitableProcessShared.LocalPolling(UniqueName(), shared, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10))
                                                                       ._tap(_disposables.Add),
            Implementation.GlobalSignalingMutex => IAwaitableProcessShared.GlobalSignaling(UniqueName(), shared, lockTimeout, waitTimeout)
                                                                          ._tap(_disposables.Add),
            Implementation.LocalSignalingMutex => IAwaitableProcessShared.LocalSignaling(UniqueName(), shared, lockTimeout, waitTimeout)
                                                                         ._tap(_disposables.Add),
            Implementation.InterprocessObjectMemoryMapped => IInterprocessObject.Create(UniqueName(), new SharedTestValueSerializer(), () => shared, CorruptionAction.ThrowException, maxCapacityInBytes: 4 * 1024, lockTimeout, waitTimeout)
                                                                                ._tap(_interprocessObjects.Add),
            Implementation.InterprocessObjectFileBacked => IInterprocessObject.CreateFileBacked(UniqueName(), new SharedTestValueSerializer(), () => shared, CorruptionAction.ThrowException, lockTimeout, waitTimeout)
                                                                              ._tap(_interprocessObjects.Add),
            _ => throw new ArgumentOutOfRangeException()
         };
      }

      static string UniqueName() => $"{typeof(TTest).FullName}.{Guid.NewGuid()}";

      public void Dispose()
      {
         foreach(var obj in _interprocessObjects)
         {
            obj.Delete();
            obj.Dispose();
         }

         foreach(var disposable in _disposables)
            disposable.Dispose();
      }
   }
}
