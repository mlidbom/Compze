using Compze.Threading.Interprocess;
using Compze.xUnitMatrix;

namespace Compze.Threading.Specifications.TestInfrastructure;

class AwaitableLockFactory<TTest> : IDisposable
{
   // ReSharper disable once StaticMemberInGenericType
   static long _counter;
   readonly List<IDisposable> _disposables = [];

   // ReSharper disable once MemberCanBeMadeStatic.Local
   AwaitableLockImplementation CurrentImplementation => (AwaitableLockImplementation)ComponentCombination.Current.Components[0];

   public IAwaitableLock CreateAwaitableLock(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
   {
      return CurrentImplementation switch
      {
         AwaitableLockImplementation.Monitor => IAwaitableMonitor.New(lockTimeout, waitTimeout),
         AwaitableLockImplementation.Mutex => CreateAwaitableMutex(lockTimeout, waitTimeout),
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   IPollingAwaitableMutex CreateAwaitableMutex(LockTimeout? lockTimeout, WaitTimeout? waitTimeout)
   {
      var uniqueName = $"{typeof(TTest).FullName}.{Interlocked.Increment(ref _counter)}";
      var mutex = IPollingAwaitableMutex.GlobalNamed(uniqueName, lockTimeout, waitTimeout, PollingInterval.Milliseconds(10));
      _disposables.Add(mutex);
      return mutex;
   }

   public void Dispose()
   {
      foreach(var disposable in _disposables)
         disposable.Dispose();
   }
}
