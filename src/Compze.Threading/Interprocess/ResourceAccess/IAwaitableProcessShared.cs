using Compze.Internals.SystemCE.Core.IOCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Factory for creating <see cref="IAwaitableProcessShared{TShared}"/> instances that protect a shared object with a cross-process <see cref="IAwaitableMutex"/>.</summary>
public partial interface IAwaitableProcessShared
{
#pragma warning disable CA2000
   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a global <see cref="IPollingAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> GlobalPolling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Global(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a local <see cref="IPollingAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> LocalPolling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutexException = null) =>
      New(shared, IPollingAwaitableMutex.Local(name, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutexException));

   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a global <see cref="ISignalingAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> GlobalSignaling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      New(shared, ISignalingAwaitableMutex.Global(name, lockTimeout, waitTimeout, onAbandonedMutexException));

   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> using a local <see cref="ISignalingAwaitableMutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> LocalSignaling<TShared>(string name, TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutexException = null) =>
      New(shared, ISignalingAwaitableMutex.Local(name, lockTimeout, waitTimeout, onAbandonedMutexException));

#pragma warning restore CA2000

   ///<summary>Returns a new <see cref="IFileBackedProcessShared{TShared}"/> that persists the shared object to a regular file, synchronized with a global <see cref="ISignalingAwaitableMutex"/>.
   ///<para>Every read and update performs filesystem I/O (full read or write of the file). The file size matches the serialized data — there is no size limit.</para>
   ///<para>Use this when simplicity matters more than throughput, or when the shared object may grow without a predictable upper bound.
   /// For high-frequency access, consider <see cref="GlobalMemoryMappedFileBacked{TShared}"/> instead.</para>
   ///</summary>
   public static IFileBackedProcessShared<TShared> GlobalFileBacked<TShared>(string name, ISharedObjectSerializer<TShared> serializer, Func<TShared> createDefault, CorruptionAction corruptionAction) where TShared : class
      => new FileBackedProcessShared<TShared>(name, fileName => DataDirectory.Value.GetOrCreateBinaryFile(fileName), serializer, createDefault, corruptionAction);

   ///<summary>Returns a new <see cref="IFileBackedProcessShared{TShared}"/> that persists the shared object to a memory-mapped file, synchronized with a global <see cref="ISignalingAwaitableMutex"/>.
   ///<para>Reads and writes are direct memory copies — no filesystem I/O per operation — making this significantly faster than <see cref="GlobalFileBacked{TShared}"/> for frequent access.
   /// Like the regular file-backed version, the data is persisted to a real file on disk and survives process restarts and reboots.</para>
   ///<para><b>WARNING:</b> <paramref name="maxCapacityInBytes"/> is a hard ceiling in bytes. If the serialized object exceeds this size, writes will throw <see cref="InvalidOperationException"/>.
   /// The backing file on disk is always allocated at the full <paramref name="maxCapacityInBytes"/> size, regardless of how much data is actually stored.</para>
   ///<para>This is a safety limit, not a performance tuning knob. Unused capacity has negligible cost — the OS only commits physical memory for pages actually written.
   /// Really, the only real meaningful constraint is when serialization time becomes a problem in your specific usage scenario.
   /// Set it comfortably above your worst-case serialized size.</para>
   ///</summary>
   public static IFileBackedProcessShared<TShared> GlobalMemoryMappedFileBacked<TShared>(string name, ISharedObjectSerializer<TShared> serializer, Func<TShared> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes) where TShared : class
      => new FileBackedProcessShared<TShared>(name, fileName => new MemoryMappedBinaryFile(DataDirectory.Value.GetFilePath(fileName + ".mmf"), maxCapacityInBytes), serializer, createDefault, corruptionAction);

   ///<summary>Returns a new <see cref="IAwaitableProcessShared{TShared}"/> that protects <paramref name="shared"/> with the supplied <paramref name="mutex"/>.</summary>
   public static IAwaitableProcessShared<TShared> New<TShared>(TShared shared, IAwaitableMutex mutex) =>
      new AwaitableProcessShared<TShared>(shared, mutex);

   internal class AwaitableProcessShared<TShared>(TShared shared, IAwaitableMutex mutex) : IAwaitableShared.AwaitableShared<TShared>(shared, mutex), IAwaitableProcessShared<TShared>
   {
      public IAwaitableMutex Mutex { get; } = mutex;
   }
}

///<summary>An <see cref="IAwaitableShared{TShared}"/> backed by a cross-process <see cref="IAwaitableMutex"/>.</summary>
public interface IAwaitableProcessShared<out TShared> : IAwaitableShared<TShared>
{
   ///<summary>The <see cref="IAwaitableMutex"/> used to protect access.</summary>
   IAwaitableMutex Mutex { get; }
}
