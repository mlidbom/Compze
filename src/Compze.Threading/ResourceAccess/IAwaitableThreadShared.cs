namespace Compze.Threading.ResourceAccess;

public interface IAwaitableThreadShared<out TShared> : IAwaitableShared<TShared>
{
   IAwaitableMonitor Monitor { get; }
}

public interface IAwaitableThreadShared
{
   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      new AwaitableThreadShared<TShared>(shared, IAwaitableMonitor.New(lockTimeout, waitTimeout));

   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, IAwaitableMonitor @lock) =>
      new AwaitableThreadShared<TShared>(shared, @lock);

   internal class AwaitableThreadShared<TShared>(TShared shared, IAwaitableMonitor monitor) : IAwaitableShared.AwaitableShared<TShared>(shared, monitor), IAwaitableThreadShared<TShared>
   {
      public IAwaitableMonitor Monitor { get; } = monitor;
   }
}
