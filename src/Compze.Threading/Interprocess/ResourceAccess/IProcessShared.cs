using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

public interface IProcessShared
{
#pragma warning disable CA2000
   public static IProcessShared<TShared> Global<TShared>(string name, TShared shared, LockTimeout? timeout, Action? onAbandonedMutexException) =>
      New(shared, IMutex.Global(name, timeout, onAbandonedMutexException));

   public static IProcessShared<TShared> Local<TShared>(string name, TShared shared, LockTimeout? timeout, Action? onAbandonedMutexException) =>
      New(shared, IMutex.Local(name, timeout, onAbandonedMutexException));
#pragma warning restore CA2000

   public static IProcessShared<TShared> New<TShared>(TShared shared, IMutex @lock) =>
      new ProcessShared<TShared>(shared, @lock);

   internal class ProcessShared<TShared>(TShared shared, IMutex mutex) : IShared.Shared<TShared>(shared, mutex), IProcessShared<TShared>
   {
      public IMutex Mutex => (IMutex)Lock;
   }
}

public interface IProcessShared<out TShared> : IShared<TShared>
{
   IMutex Mutex { get; }
}
