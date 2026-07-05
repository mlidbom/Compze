using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Factory for creating <see cref="IProcessShared{TShared}"/> instances that protect a shared object with a cross-process <see cref="IMutex"/>.</summary>
public interface IProcessShared
{
#pragma warning disable CA2000 // Mutex ownership transfers to ProcessShared which disposes it
   ///<summary>Returns a new <see cref="IProcessShared{TShared}"/> using a global <see cref="IMutex"/> that synchronizes across all user sessions.</summary>
   public static IProcessShared<TShared> Global<TShared>(string name, TShared shared, LockTimeout? timeout = null, Action? onAbandonedMutex = null) =>
      new ProcessShared<TShared>(shared, IMutex.Global(name, timeout, onAbandonedMutex));

   ///<summary>Returns a new <see cref="IProcessShared{TShared}"/> using a local <see cref="IMutex"/> that synchronizes within a single user session.</summary>
   public static IProcessShared<TShared> Local<TShared>(string name, TShared shared, LockTimeout? timeout = null, Action? onAbandonedMutex = null) =>
      new ProcessShared<TShared>(shared, IMutex.Local(name, timeout, onAbandonedMutex));
#pragma warning restore CA2000

   internal class ProcessShared<TShared>(TShared shared, IMutex mutex) : IShared.Shared<TShared>(shared, mutex), IProcessShared<TShared>
   {
      public IMutex Mutex => (IMutex)CriticalSection;
      public void Dispose() => Mutex.Dispose();
   }
}

///<summary>An <see cref="IShared{TShared}"/> backed by a cross-process <see cref="IMutex"/>.</summary>
public interface IProcessShared<out TShared> : IShared<TShared>, IDisposable
{
   ///<summary>The <see cref="IMutex"/> used to protect access.</summary>
   IMutex Mutex { get; }
}
