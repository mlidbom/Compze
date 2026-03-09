namespace Compze.Threading.ResourceAccess;

public interface IThreadShared<out TShared> : IShared<TShared>
{
}

public interface IThreadShared
{
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new ThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   public static IThreadShared<TShared> New<TShared>(TShared shared, ILock @lock) =>
      new ThreadShared<TShared>(shared, @lock);

   internal class ThreadShared<TShared>(TShared shared, ILock @lock) : IThreadShared<TShared>
   {
      readonly TShared _shared = shared;
      public ILock Lock { get; } = @lock;

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) =>
         Lock.Locked(() => func(_shared), timeout);
   }
}
