using Compze.Threading.Interprocess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

partial class ICriticalSectionMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      // ReSharper disable once StaticMemberInGenericType
      static long _counter;
      readonly List<IDisposable> _disposables = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public ICriticalSection Create(LockTimeout? timeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor     => IMonitor.New(timeout),
            Implementation.GlobalMutex => UniqueName()._(it => IMutex.Global(it, timeout))._tap(_disposables.Add),
            Implementation.LocalMutex  => UniqueName()._(it => IMutex.Local(it, timeout))._tap(_disposables.Add),
            _                          => throw new ArgumentOutOfRangeException()
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
