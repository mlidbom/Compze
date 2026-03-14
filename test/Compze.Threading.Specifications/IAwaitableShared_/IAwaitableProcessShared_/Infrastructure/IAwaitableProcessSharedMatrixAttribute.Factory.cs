using Compze.Internals.SystemCE.LinqCE;
using Compze.InterprocessObject;
using Compze.InterprocessObject.MemoryPack;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.IAwaitableShared_.IAwaitableProcessShared_.Infrastructure;

partial class IAwaitableProcessSharedMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
      readonly List<IDisposable> _disposables = [];
      readonly List<IInterprocessObject<SharedTestValue>> _interprocessObjects = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public bool CurrentImplementationIsGlobal => CurrentImplementation is Implementation.GlobalMutex
                                                                                                              or Implementation.GlobalInterprocessObject;

      public IAwaitableProcessShared<SharedTestValue> Create(SharedTestValue shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.GlobalMutex => IAwaitableProcessShared.Global(UniqueName(), TestDirectory, shared, lockTimeout, waitTimeout)
                                                                        ._tap(_disposables.Add),
            Implementation.LocalMutex => IAwaitableProcessShared.Local(UniqueName(), TestDirectory, shared, lockTimeout, waitTimeout)
                                                                       ._tap(_disposables.Add),
            Implementation.GlobalInterprocessObject => MemoryPackInterprocessObject.NewGlobal(UniqueName(), () => shared, CorruptionAction.ThrowException, maxBytes: 4 * 1024, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
                                                                                               ._tap(_interprocessObjects.Add),
            Implementation.LocalInterprocessObject => MemoryPackInterprocessObject.NewLocal(UniqueName(), () => shared, CorruptionAction.ThrowException, maxBytes: 4 * 1024, TestDirectory, lockTimeout: lockTimeout, waitTimeout: waitTimeout)
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
