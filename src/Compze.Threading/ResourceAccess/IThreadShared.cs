namespace Compze.Threading.ResourceAccess;

public interface IThreadShared<out TShared> : IShared<TShared>
{
   IMonitor Monitor { get; }
}

public interface IThreadShared
{
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new ThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   public static IThreadShared<TShared> New<TShared>(TShared shared, IMonitor @lock) =>
      new ThreadShared<TShared>(shared, @lock);

   internal class ThreadShared<TShared>(TShared shared, IMonitor monitor) : IShared.Shared<TShared>(shared, monitor), IThreadShared<TShared>
   {
      public IMonitor Monitor { get; } = monitor;
   }
}
