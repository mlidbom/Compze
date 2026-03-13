using Compze.Internals.SystemCE.Core.IOCE;
using Compze.Threading;

namespace Compze.InterprocessObject;

///<summary>Factory for creating <see cref="IInterprocessObject{T}"/> instances — strongly-typed objects shared across processes via memory-mapped files.</summary>
public partial interface IInterprocessObject
{
   static readonly Lazy<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                          .LocalApplicationData
                                                                          .GetOrCreateDirectory("Compze")
                                                                          .GetOrCreateDirectory("SharedFiles"));

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a global cross-process mutex.
   ///<para>Reads and writes are direct memory copies — no filesystem I/O per operation — making this the recommended option for most use cases.
   /// The data is persisted to a real file on disk and survives process restarts and reboots.</para>
   ///<para><b>WARNING:</b> <paramref name="maxCapacityInBytes"/> is a hard ceiling in bytes. If the serialized object exceeds this size, writes will throw <see cref="InvalidOperationException"/>.
   /// The backing file on disk is always allocated at the full <paramref name="maxCapacityInBytes"/> size, regardless of how much data is actually stored.</para>
   ///<para>This is a safety limit, not a performance tuning knob. Unused capacity has negligible cost — the OS only commits physical memory for pages actually written.
   /// Set it comfortably above your worst-case serialized size.
   /// Really, the only real meaningful constraint is when serialization time becomes a problem in your specific usage scenario.</para>
   ///</summary>
   public static IInterprocessObject<T> NewGlobal<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => CreateInternal(name, isGlobal: true, serializer, createDefault, corruptionAction, maxCapacityInBytes, new DirectoryCE(directory), lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a session-local cross-process mutex.</summary>
   public static IInterprocessObject<T> NewLocal<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => CreateInternal(name, isGlobal: false, serializer, createDefault, corruptionAction, maxCapacityInBytes, new DirectoryCE(directory), lockTimeout, waitTimeout);

   static IInterprocessObject<T> CreateInternal<T>(string name, bool isGlobal, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes, DirectoryCE directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout) where T : class
      => new InterprocessObjectImplementation<T>(name, isGlobal, directory.GetDirectoryInfo(), fileName => new MemoryMappedBinaryFile(directory.GetFilePath(fileName + ".mmf"), maxCapacityInBytes), serializer, createDefault, corruptionAction, lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a regular file, synchronized with a global cross-process mutex.
   ///<para>Every read and update performs filesystem I/O (full read or write of the file). The file size matches the serialized data — there is no size limit.</para>
   ///<para>Use this when simplicity matters more than throughput, or when the shared object may grow without a predictable upper bound.
   /// For high-frequency access, consider <see cref="NewGlobal{T}"/> instead.</para>
   ///</summary>
   public static IInterprocessObject<T> NewGlobalFileBacked<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => CreateFileBackedInternal(name, isGlobal: true, serializer, createDefault, corruptionAction, new DirectoryCE(directory), lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a regular file, synchronized with a session-local cross-process mutex.</summary>
   public static IInterprocessObject<T> NewLocalFileBacked<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => CreateFileBackedInternal(name, isGlobal: false, serializer, createDefault, corruptionAction, new DirectoryCE(directory), lockTimeout, waitTimeout);

   static IInterprocessObject<T> CreateFileBackedInternal<T>(string name, bool isGlobal, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, DirectoryCE directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout) where T : class
      => new InterprocessObjectImplementation<T>(name, isGlobal, directory.GetDirectoryInfo(), fileName => directory.GetOrCreateBinaryFile(fileName), serializer, createDefault, corruptionAction, lockTimeout, waitTimeout);
}
