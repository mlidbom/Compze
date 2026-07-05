using Compze.Threading;
using Compze.Threading.Interprocess;

namespace Compze.InterprocessObject;

///<summary>Factory for creating <see cref="IInterprocessObject{T}"/> instances — strongly-typed objects shared across processes via memory-mapped files.</summary>
public partial interface IInterprocessObject
{
   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a global cross-process mutex.
   ///<para><b>WARNING:</b> <paramref name="maxBytes"/> is a hard ceiling in bytes. If the serialized object exceeds this size, writes will throw <see cref="InvalidOperationException"/>.
   /// The backing file on disk is always allocated at the full <paramref name="maxBytes"/> size, regardless of how much data is actually stored.</para>
   ///<para>This is a safety limit, not a performance tuning knob. Unused capacity has negligible cost — the OS only commits physical memory for pages actually written.
   /// Set it comfortably above your worst-case serialized size.
   /// Really, the only real meaningful constraint is when serialization time becomes a problem in your specific usage scenario.</para>
   ///</summary>
   public static IInterprocessObject<T> NewGlobal<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, ISignalPollingPolicy? signalPollingPolicy = null) where T : class
      => CreateInternal(name, isGlobal: true, serializer, createDefault, corruptionAction, maxBytes, directory, lockTimeout, waitTimeout, signalPollingPolicy);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a session-local cross-process mutex.</summary>
   public static IInterprocessObject<T> NewLocal<T>(string name, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, ISignalPollingPolicy? signalPollingPolicy = null) where T : class
      => CreateInternal(name, isGlobal: false, serializer, createDefault, corruptionAction, maxBytes, directory, lockTimeout, waitTimeout, signalPollingPolicy);

   private static IInterprocessObject<T> CreateInternal<T>(string name, bool isGlobal, IInterprocessObjectSerializer<T> serializer, Func<T> createDefault, CorruptionAction corruptionAction, int maxBytes, DirectoryInfo directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, ISignalPollingPolicy? signalPollingPolicy) where T : class
      => new Implementation<T>(name, isGlobal, directory, maxBytes, serializer, createDefault, corruptionAction, lockTimeout, waitTimeout, signalPollingPolicy);
}
