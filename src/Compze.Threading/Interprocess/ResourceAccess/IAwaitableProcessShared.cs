using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

public interface IAwaitableProcessShared
{
#pragma warning disable CA2000
   public static IAwaitableProcessShared<TShared> Global<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Global(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> Local<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Local(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

#pragma warning restore CA2000

   public static IAwaitableProcessShared<TShared> New<TShared>(TShared shared, IPollingAwaitableMutex mutex) =>
      new AwaitableProcessShared<TShared>(shared, mutex);

   internal class AwaitableProcessShared<TShared>(TShared shared, IPollingAwaitableMutex mutex) : IAwaitableShared.AwaitableShared<TShared>(shared, mutex), IAwaitableProcessShared<TShared>
   {
      public IPollingAwaitableMutex Mutex { get; } = mutex;
   }
}

public interface IAwaitableProcessShared<out TShared> : IAwaitableShared<TShared>
{
   IPollingAwaitableMutex Mutex { get; }
}
