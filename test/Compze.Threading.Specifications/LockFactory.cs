using Compze.Threading.Interprocess;
using Compze.Threading.ResourceAccess;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications;

class LockFactory<TTest> : IDisposable
{
   // ReSharper disable once StaticMemberInGenericType
   static long _counter;
   readonly List<IDisposable> _disposables = [];

   // ReSharper disable once MemberCanBeMadeStatic.Local
   LockImplementation CurrentLockImplementation => (LockImplementation)ComponentCombination.Current.Components[0];

   public ILock CreateLock(LockTimeout? timeout = null)
   {
      return CurrentLockImplementation switch
      {
         LockImplementation.Monitor => ILock.New(timeout),
         LockImplementation.Mutex => CreateMutex(timeout),
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   IMutex CreateMutex(LockTimeout? timeout)
   {
      var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
      var mutex = IMutex.GlobalNamed(uniqueName, timeout);
      _disposables.Add(mutex);
      return mutex;
   }

   public void Dispose()
   {
      foreach(var disposable in _disposables)
         disposable.Dispose();
   }
}
