namespace Compze.Threading.ResourceAccess;

public interface IThreadShared<out TShared>
{
   IMonitor Monitor { get; }

   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}

public interface IThreadShared
{
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new ThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   public static IThreadShared<TShared> New<TShared>(TShared shared, IMonitor monitor) =>
      new ThreadShared<TShared>(shared, monitor);

   class ThreadShared<TShared>(TShared shared, IMonitor monitor) : IThreadShared<TShared>
   {
      readonly TShared _shared = shared;
      public IMonitor Monitor { get; } = monitor;

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) =>
         Monitor.Locked(() => func(_shared), timeout);
   }
}
