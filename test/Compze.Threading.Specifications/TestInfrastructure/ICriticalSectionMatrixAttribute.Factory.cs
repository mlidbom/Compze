using Compze.Threading.Interprocess;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

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
            Implementation.Monitor => IMonitor.New(timeout),
            Implementation.GlobalMutex => CreateMutex(global: true, timeout),
            Implementation.LocalMutex => CreateMutex(global: false, timeout),
            _ => throw new ArgumentOutOfRangeException()
         };
      }

      IMutex CreateMutex(bool global, LockTimeout? timeout)
      {
         var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
         var mutex = global ? IMutex.Global(uniqueName, timeout) : IMutex.Local(uniqueName, timeout);
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
