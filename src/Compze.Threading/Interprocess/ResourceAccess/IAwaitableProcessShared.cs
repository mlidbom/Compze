using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

public partial interface IAwaitableProcessShared
{
#pragma warning disable CA2000
   public static IAwaitableProcessShared<TShared> GlobalPolling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Global(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> LocalPolling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Local(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> GlobalSignaling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      New(shared, ISignalingAwaitableMutex.Global(name, lockTimeout, waitTimeout, onAbandonedMutexException));

   public static IAwaitableProcessShared<TShared> LocalSignaling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      New(shared, ISignalingAwaitableMutex.Local(name, lockTimeout, waitTimeout, onAbandonedMutexException));

#pragma warning restore CA2000

   public static IFileBackedProcessShared<TShared> GlobalFileBacked<TShared>(string name, ISharedObjectSerializer<TShared> serializer, Func<TShared> createDefault,CorruptionAction corruptionAction) where TShared : class
      => new FileBackedProcessShared<TShared>(name, serializer, createDefault, corruptionAction);

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
