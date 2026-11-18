using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.MemoryImageStateStore._private;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Composes a memory image state store into a container: a memory image of taggregates, every one held in memory for the<br/>
/// container's life and rebuilt at its start from a snapshot of each, which its storage keeps.</summary>
public static class MemoryImageStateStoreRegistrar
{
   ///<summary>Composes a memory image state store: a memory image of taggregates, every one held in memory for the container's life and<br/>
   /// rebuilt at its start from a snapshot of each, which the storage <paramref name="declare"/> declares keeps, if it declares any. It<br/>
   /// keeps taggregates as their state, not their history. Its taggregates are read through the container's <see cref="ITaggregateStoreReader"/>,<br/>
   /// and changed through each unit of work's <see cref="ITaggregateStoreUpdater"/>. The tevents they publish are dispatched through<br/>
   /// the container's Tessaging engine, such as a LocalTessagingEngine, as each publish cascade ends.</summary>
   ///<remarks><b>Its memory is its whole data set,</b> and what it costs follows from holding every taggregate:<br/>
   /// A read, through <see cref="ITaggregateStoreReader"/> or of a reference's <see cref="ITaggregateReference{TTaggregate}.Current"/>, gives<br/>
   /// every reader the one committed instance the store holds, read only: a lookup in memory, with no lock and no copy.<br/>
   /// A get for update copies the taggregate through its snapshot (<see cref="ISnapshotable{TSelf,TSnapshot}"/>), so it costs in<br/>
   /// proportion to the taggregate's whole state, and takes the taggregate's lock, which the unit of work holds until it completes.<br/>
   /// Another unit of work that gets the same taggregate for update waits at most 30 seconds, then throws a <see cref="TimeoutException"/><br/>
   /// naming the taggregate. A unit of work that waits for a lock while it holds one can deadlock with another, until one of them gives up.<br/>
   /// <see cref="ITaggregateStoreBulkReader.ReferencesToAll{TTaggregate}"/> is a scan of what the store holds, and each index,<br/>
   /// <see cref="ITaggregateIndex{TTaggregate,TKey}"/>, files every taggregate of its class that the store holds.</remarks>
   ///<remarks><b>It loads every taggregate at its first use,</b> of any kind: a read, a reference's <see cref="ITaggregateReference{TTaggregate}.Current"/>,<br/>
   /// a lookup, a bulk read, or a unit of work's first get for update or add. The load runs on that use's thread, and for a unit of<br/>
   /// work inside its transaction, so a long load can outlast the transaction's timeout. Other threads that use the store meanwhile<br/>
   /// wait for the load, for at most Compze.Threading's <see cref="LockTimeout.Default"/>, then throw. To choose when the store loads,<br/>
   /// and on which thread, call <see cref="IMemoryImageStateStore.LoadFromStorage"/> at the application's start. What a later load waits<br/>
   /// for is in <see cref="IMemoryImageStateStore"/>'s documentation, and what the store does when a commit of its files fails is in<br/>
   /// <see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>'s.</remarks>
   ///<remarks>A taggregate's <see cref="ITaggregate.Version"/> counts the changes since the store loaded it, not its whole history: each<br/>
   /// load restores it at version 1, so the <see cref="ITaggregateTevent.TaggregateVersion"/> of its tevents repeats across loads.</remarks>
   public static IComponentRegistrar MemoryImageStateStore(this IComponentRegistrar @this, Action<MemoryImageStateStoreBuilder> declare)
   {
      var builder = new MemoryImageStateStoreBuilder(@this);
      declare(builder);
      builder.AssertDeclarationsFitTogether();
      @this.Register(Singleton.For<IMemoryImageStateStore, TaggregateMemoryImageStateStore>().CreatedBy((TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver) => builder.Build(unitsOfWork, resolver)));
      @this.RegisterTaggregateStore<TaggregateMemoryImageStateStore>();
      return @this;
   }
}
