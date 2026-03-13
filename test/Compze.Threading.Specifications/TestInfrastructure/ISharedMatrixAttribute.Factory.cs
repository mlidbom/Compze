using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.ResourceAccess;
using Compze.Underscore;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

partial class ISharedMatrixAttribute
{
   public class Factory<TTest> : IDisposable
   {
      // ReSharper disable once StaticMemberInGenericType
      static long _counter;
      readonly List<IDisposable> _disposables = [];

      // ReSharper disable once MemberCanBeMadeStatic.Local
      Implementation CurrentImplementation => (Implementation)MatrixCombination.Current.Components[0];

      public IShared<TShared> Create<TShared>(TShared shared, LockTimeout? timeout = null)
      {
         return CurrentImplementation switch
         {
            Implementation.Monitor     => IThreadShared.New(shared, timeout),
            Implementation.GlobalMutex => IProcessShared.Global(UniqueName(), shared, timeout)._tap(_disposables.Add),
            Implementation.LocalMutex  => IProcessShared.Local(UniqueName(), shared, timeout)._tap(_disposables.Add),
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
