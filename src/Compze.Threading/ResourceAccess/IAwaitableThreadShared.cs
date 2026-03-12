namespace Compze.Threading.ResourceAccess;

///<summary>An <see cref="IAwaitableShared{TShared}"/> backed by an in-process <see cref="IAwaitableMonitor"/>.</summary>
public interface IAwaitableThreadShared<out TShared> : IAwaitableShared<TShared>
{
   ///<summary>The <see cref="IAwaitableMonitor"/> used to protect access.</summary>
   IAwaitableMonitor Monitor { get; }
}

///<summary>Factory for creating <see cref="IAwaitableThreadShared{TShared}"/> instances.</summary>
public interface IAwaitableThreadShared
{
   ///<summary>Returns a new <see cref="IAwaitableThreadShared{TShared}"/> that protects <paramref name="shared"/> with a new <see cref="IAwaitableMonitor"/>.</summary>
   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      new AwaitableThreadShared<TShared>(shared, IAwaitableMonitor.New(lockTimeout, waitTimeout));

   ///<summary>Returns a new <see cref="IAwaitableThreadShared{TShared}"/> that protects <paramref name="shared"/> with the supplied <paramref name="lock"/>.</summary>
   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, IAwaitableMonitor @lock) =>
      new AwaitableThreadShared<TShared>(shared, @lock);

   internal class AwaitableThreadShared<TShared>(TShared shared, IAwaitableMonitor monitor) : IAwaitableShared.AwaitableShared<TShared>(shared, monitor), IAwaitableThreadShared<TShared>
   {
      public IAwaitableMonitor Monitor { get; } = monitor;
   }
}
