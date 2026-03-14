using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Factory for creating <see cref="IAwaitableProcessShared{TShared}"/> instances that protect a shared object with a cross-process <see cref="IAwaitableMutex"/>.
///<para><b>NOTE:</b> The shared object is NOT shared across processes — each process holds its own instance in its own memory space.
/// The mutex coordinates <em>timing</em> so only one process enters the critical section at a time.
/// This is useful for protecting access to an external resource (file, port, database) rather than for sharing data between processes.
/// For genuine cross-process shared state, use <c>Compze.InterprocessObject</c> instead.</para>
///</summary>
public partial interface IAwaitableProcessShared
{
#pragma warning disable CA2000 // Mutex ownership transfers to AwaitableProcessShared which disposes it
   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a global <see cref="IAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> Global<TShared>(string name, DirectoryInfo directory, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      new AwaitableProcessShared<TShared>(shared, IAwaitableMutex.Global(name, directory, lockTimeout, waitTimeout, onAbandonedMutexException));

   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a local <see cref="IAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> Local<TShared>(string name, DirectoryInfo directory, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      new AwaitableProcessShared<TShared>(shared, IAwaitableMutex.Local(name, directory, lockTimeout, waitTimeout, onAbandonedMutexException));
#pragma warning restore CA2000

   internal class AwaitableProcessShared<TShared>(TShared shared, IAwaitableMutex mutex) : IAwaitableShared.AwaitableShared<TShared>(shared, mutex), IAwaitableProcessShared<TShared>
   {
      public IAwaitableMutex Mutex { get; } = mutex;
      public void Dispose() => Mutex.Dispose();
   }
}

///<summary>An <see cref="IAwaitableShared{TShared}"/> backed by a cross-process <see cref="IAwaitableMutex"/>.</summary>
public interface IAwaitableProcessShared<out TShared> : IAwaitableShared<TShared>, IDisposable
{
   ///<summary>The <see cref="IAwaitableMutex"/> used to protect access.</summary>
   IAwaitableMutex Mutex { get; }
}
