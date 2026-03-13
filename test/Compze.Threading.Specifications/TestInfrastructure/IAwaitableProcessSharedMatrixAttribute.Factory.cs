using Compze.Internals.SystemCE.LinqCE;
using Compze.InterprocessObject;
using Compze.InterprocessObject.MemoryPack;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

partial class IAwaitableProcessSharedMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
      readonly List<IDisposable> _disposables = [];
      readonly List<IInterprocessObject<SharedTestValue>> _interprocessObjects = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public bool CurrentImplementationIsGlobal => CurrentImplementation is Implementation.GlobalPollingMutex or Implementation.GlobalSignalingMutex
                                                                                                              or Implementation.GlobalInterprocessObjectMemoryMapped or Implementation.GlobalInterprocessObjectFileBacked;

      public IAwaitableProcessShared<SharedTestValue> Create(SharedTestValue shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.GlobalPollingMutex => IAwaitableProcessShared.GlobalPolling(UniqueName(), shared, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10))
                                                                        ._tap(_disposables.Add),
            Implementation.LocalPollingMutex => IAwaitableProcessShared.LocalPolling(UniqueName(), shared, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10))
                                                                       ._tap(_disposables.Add),
            Implementation.GlobalSignalingMutex => IAwaitableProcessShared.GlobalSignaling(UniqueName(), TestDirectory, shared, lockTimeout, waitTimeout)
                                                                          ._tap(_disposables.Add),
            Implementation.LocalSignalingMutex => IAwaitableProcessShared.LocalSignaling(UniqueName(), TestDirectory, shared, lockTimeout, waitTimeout)
                                                                         ._tap(_disposables.Add),
            Implementation.GlobalInterprocessObjectMemoryMapped => MemoryPackInterprocessObject.NewGlobal(UniqueName(), () => shared, CorruptionAction.ThrowException, maxCapacityInBytes: 4 * 1024, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
                                                                                               ._tap(_interprocessObjects.Add),
            Implementation.GlobalInterprocessObjectFileBacked => MemoryPackInterprocessObject.NewGlobalFileBacked(UniqueName(), () => shared, CorruptionAction.ThrowException, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
                                                                                             ._tap(_interprocessObjects.Add),
            Implementation.LocalInterprocessObjectMemoryMapped => MemoryPackInterprocessObject.NewLocal(UniqueName(), () => shared, CorruptionAction.ThrowException, maxCapacityInBytes: 4 * 1024, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
                                                                                              ._tap(_interprocessObjects.Add),
            Implementation.LocalInterprocessObjectFileBacked => MemoryPackInterprocessObject.NewLocalFileBacked(UniqueName(), () => shared, CorruptionAction.ThrowException, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
                                                                                            ._tap(_interprocessObjects.Add),
            _ => throw new ArgumentOutOfRangeException()
         };
      }

      static string UniqueName() => $"{typeof(TTest).FullName}.{Guid.NewGuid()}";

      public void Dispose()
      {
         _interprocessObjects.ForEach(it => it.Delete());
         _disposables.Concat(_interprocessObjects)
                     .ForEach(it => it.Dispose());
      }
   }
}
