namespace Compze.Threading.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new ThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   public static IThreadShared<TShared> New<TShared>(TShared shared, IMonitor monitor) =>
      new ThreadShared<TShared>(shared, monitor);

   // Single implementation for both IThreadShared<T> and IAwaitableThreadShared<T>.
   class ThreadShared<TShared> : IThreadShared<TShared>, IAwaitableThreadShared<TShared>
   {
      readonly TShared _shared;
      readonly IAwaitableMonitor _awaitableMonitor;

      public IMonitor Monitor { get; }
      IAwaitableMonitor IAwaitableThreadShared<TShared>.Monitor => _awaitableMonitor;

      internal ThreadShared(TShared shared, IMonitor monitor)
      {
         _shared = shared;
         Monitor = monitor;
         _awaitableMonitor = (IAwaitableMonitor)monitor;
      }

      internal ThreadShared(TShared shared, IAwaitableMonitor awaitableMonitor)
      {
         _shared = shared;
         Monitor = (IMonitor)awaitableMonitor;
         _awaitableMonitor = awaitableMonitor;
      }

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) => Monitor.Locked(() => func(_shared), timeout);

      public TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null) => _awaitableMonitor.Read(() => read(_shared), timeout);

      public TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null) => _awaitableMonitor.ReadWhen(() => condition(_shared), () => read(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null) => _awaitableMonitor.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null) => _awaitableMonitor.UpdateWhen(() => condition(_shared), () => update(_shared), timeout);
   }
}

public interface IThreadShared<out TShared>
{
   IMonitor Monitor { get; }

   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}
