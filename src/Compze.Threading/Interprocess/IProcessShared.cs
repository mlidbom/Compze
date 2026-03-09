using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess;

public interface IProcessShared : IThreadShared
{
   public static IProcessShared<TShared> Global<TShared>(string name, TShared shared, LockTimeout? timeout, Action? onAbandonedMutexException) =>
      New(shared, IMutex.GlobalNamed(name, timeout, onAbandonedMutexException));

   public static IProcessShared<TShared> Local<TShared>(string name, TShared shared, LockTimeout? timeout, Action? onAbandonedMutexException) =>
      New(shared, IMutex.LocalNamed(name, timeout, onAbandonedMutexException));

   public static IProcessShared<TShared> New<TShared>(TShared shared, IMutex @lock) =>
      new ProcessShared<TShared>(shared, @lock);

   internal class ProcessShared<TShared>(TShared shared, IMutex @lock) : ThreadShared<TShared>(shared, @lock), IProcessShared<TShared> {}
}

public interface IProcessShared<out TShared> : IThreadShared<TShared> {}
