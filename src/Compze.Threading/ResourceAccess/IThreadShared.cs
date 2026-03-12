namespace Compze.Threading.ResourceAccess;

///<summary>An <see cref="IShared{TShared}"/> backed by an in-process <see cref="IMonitor"/>.</summary>
public interface IThreadShared<out TShared> : IShared<TShared>
{
   ///<summary>The <see cref="IMonitor"/> used to protect access.</summary>
   IMonitor Monitor { get; }
}

///<summary>Factory for creating <see cref="IThreadShared{TShared}"/> instances.</summary>
public interface IThreadShared
{
   ///<summary>Returns a new <see cref="IThreadShared{TShared}"/> that protects <paramref name="shared"/> with a new <see cref="IMonitor"/>.</summary>
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new ThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   ///<summary>Returns a new <see cref="IThreadShared{TShared}"/> that protects <paramref name="shared"/> with the supplied <paramref name="monitor"/>.</summary>
   public static IThreadShared<TShared> New<TShared>(TShared shared, IMonitor monitor) =>
      new ThreadShared<TShared>(shared, monitor);

   internal class ThreadShared<TShared>(TShared shared, IMonitor monitor) : IShared.Shared<TShared>(shared, monitor), IThreadShared<TShared>
   {
      public IMonitor Monitor { get; } = monitor;
   }
}
