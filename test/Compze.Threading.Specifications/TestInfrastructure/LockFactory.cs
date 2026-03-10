using Compze.Threading.Interprocess;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

class LockFactory<TTest> : IDisposable
{
   // ReSharper disable once StaticMemberInGenericType
   static long _counter;
   readonly List<IDisposable> _disposables = [];

   // ReSharper disable once MemberCanBeMadeStatic.Local
   LockImplementation CurrentLockImplementation => (LockImplementation)MatrixCombination.Current.Components[0];

   public ILock CreateLock(LockTimeout? timeout = null)
   {
      return CurrentLockImplementation switch
      {
         LockImplementation.Monitor => IMonitor.New(timeout),
         LockImplementation.Mutex => CreateMutex(timeout),
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   IMutex CreateMutex(LockTimeout? timeout)
   {
      var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
      var mutex = IMutex.Global(uniqueName, timeout);
      _disposables.Add(mutex);
      return mutex;
   }

   public void Dispose()
   {
      foreach(var disposable in _disposables)
         disposable.Dispose();
   }
}
