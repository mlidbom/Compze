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

   ///<summary>Returns a new <see cref="IFileBackedProcessShared{TShared}"/> that persists the shared object to a file, synchronized with a global <see cref="ISignalingAwaitableMutex"/>.</summary>
   public static IFileBackedProcessShared<TShared> GlobalFileBacked<TShared>(string name, ISharedObjectSerializer<TShared> serializer, Func<TShared> createDefault, CorruptionAction corruptionAction) where TShared : class
   {
      var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
      var synchronizer = ISignalingAwaitableMutex.Global(fileName);
      var file = synchronizer.Update(() => DataDirectory.Value.GetOrCreateBinaryFile(fileName, () => serializer.Serialize(createDefault())));
      return new FileBackedProcessShared<TShared>(synchronizer, file, serializer, createDefault, corruptionAction);
   }

   ///<summary>Returns a new <see cref="IFileBackedProcessShared{TShared}"/> that persists the shared object to a memory-mapped file, synchronized with a global <see cref="ISignalingAwaitableMutex"/>.</summary>
   public static IFileBackedProcessShared<TShared> GlobalMemoryMappedFileBacked<TShared>(string name, ISharedObjectSerializer<TShared> serializer, Func<TShared> createDefault, CorruptionAction corruptionAction, int maxCapacity = 256 * 1024) where TShared : class
   {
      var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
      var synchronizer = ISignalingAwaitableMutex.Global(fileName);
      var file = synchronizer.Update(() =>
      {
         var filePath = DataDirectory.Value.GetFilePath(fileName + ".mmf");
         var mmf = new MemoryMappedBinaryFile(filePath, maxCapacity);
         if(mmf.ReadAllBytes().Length == 0)
            mmf.WriteAllBytes(serializer.Serialize(createDefault()));
         return mmf;
      });
      return new FileBackedProcessShared<TShared>(synchronizer, file, serializer, createDefault, corruptionAction);
   }

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
