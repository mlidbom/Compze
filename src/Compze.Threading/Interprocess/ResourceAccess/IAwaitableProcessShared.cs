using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

public interface IAwaitableProcessShared
{
   public static IAwaitableProcessShared<TShared> Global<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IAwaitableMutex.GlobalNamed(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> Local<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IAwaitableMutex.LocalNamed(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> New<TShared>(TShared shared, IAwaitableMutex mutex) =>
      new AwaitableProcessShared<TShared>(shared, mutex);

   internal class AwaitableProcessShared<TShared>(TShared shared, IAwaitableMutex mutex) : IAwaitableShared.AwaitableShared<TShared>(shared, mutex), IAwaitableProcessShared<TShared>
   {
      public IAwaitableMutex Mutex { get; } = mutex;
   }
}

public interface IAwaitableProcessShared<out TShared> : IAwaitableShared<TShared>
{
   IAwaitableMutex Mutex { get; }
}
