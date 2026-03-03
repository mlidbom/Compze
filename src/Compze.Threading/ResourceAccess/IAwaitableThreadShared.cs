namespace Compze.Threading.ResourceAccess;

public interface IAwaitableThreadShared
{
   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      IAwaitableThreadShared<TShared>.New(shared, lockTimeout, waitTimeout);

   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, IAwaitableMonitor monitor) =>
      new IThreadShared.ThreadShared<TShared>(shared, monitor);
}

public interface IAwaitableThreadShared<out TShared>
{
   public static IAwaitableThreadShared<TShared> New(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      new IThreadShared.ThreadShared<TShared>(shared, IAwaitableMonitor.New(lockTimeout, waitTimeout));

   public static IAwaitableThreadShared<TShared> New(TShared shared, IAwaitableMonitor monitor) =>
      new IThreadShared.ThreadShared<TShared>(shared, monitor);

   IAwaitableMonitor Monitor { get; }

   //core
   TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null);

   //Default implementations
   unit Update(Action<TShared> update, LockTimeout? timeout = null) => Update(update.ToFunc(), timeout);
   unit UpdateWhen(Func<TShared, bool> condition, Action<TShared> update, WaitTimeout? timeout = null) => UpdateWhen(condition, update.ToFunc(), timeout);

   unit Await(Func<TShared, bool> condition, WaitTimeout? timeout = null) => ReadWhen(condition, _ => unit.Value, timeout);
}
