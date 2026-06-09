using Compze.Threading.Interprocess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;

partial class IAwaitableCriticalSectionMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      // ReSharper disable once StaticMemberInGenericType The temp directory path is identical for every TTest, so one static copy per closed generic is harmless (same as _counter below).
      static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "Signals"))._mutate(it => it.Create());

      // ReSharper disable once StaticMemberInGenericType
      static long _counter;
      readonly List<IDisposable> _disposables = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.DimensionValues[0];

      public IAwaitableCriticalSection Create(WaitTimeout waitTimeout) => Create(null, waitTimeout);
      public IAwaitableCriticalSection Create(LockTimeout lockTimeout) => Create(lockTimeout, null);

      public IAwaitableCriticalSection Create(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor    => IAwaitableMonitor.New(lockTimeout, waitTimeout),
            Implementation.GlobalMutex => UniqueName()
                                          ._(it => IAwaitableMutex.Global(it, TestDirectory, lockTimeout, waitTimeout))
                                          ._tap(_disposables.Add),
            Implementation.LocalMutex  => UniqueName()
                                          ._(it => IAwaitableMutex.Local(it, TestDirectory, lockTimeout, waitTimeout))
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
