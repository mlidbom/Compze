using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>A memory image state store, a memory image of taggregates: every one held in memory for the container's life, rebuilt at its<br/>
/// start from a snapshot of each, which its storage keeps. This is what the store does as a whole, beyond reading and changing its<br/>
/// taggregates through <see cref="ITaggregateStoreReader"/>, <see cref="ITaggregateStoreBulkReader"/> and <see cref="ITaggregateStoreUpdater"/>.<br/>
/// Resolved from the container that composes the store.</summary>
///<remarks>The store loads every taggregate at its first use, on whatever thread that is, and inside that use's transaction, if any.<br/>
/// <see cref="LoadFromStorage"/> called at the application's start is how a consumer chooses when, and on which thread: see<br/>
/// <see cref="MemoryImageStateStoreRegistrar.MemoryImageStateStore"/>.</remarks>
public interface IMemoryImageStateStore
{
   ///<summary>Loads every taggregate from the storage, and replaces the committed taggregates with what it loaded: the store's first<br/>
   /// load, when nothing has used the store yet, and otherwise a load again, as when a program other than this one has changed what the<br/>
   /// storage holds. Dispatches no tevent.<br/>
   /// With <paramref name="changeTheStorageFirst"/>, runs it first, once the open units of work have completed, while new ones wait:<br/>
   /// such as a fast-forward of the checkout that holds the consumer's files, which no unit of work may write a taggregate over.</summary>
   ///<remarks>Waits for the units of work open in the store to complete, and keeps new ones from starting until it is done. Every<br/>
   /// <see cref="ITaggregateReference{TTaggregate}"/> is kept, and gives the reloaded state; the indexes file the reloaded state.<br/>
   /// During a load again, readers don't wait: they read what was committed until the load replaces it.<br/>
   /// Throws for a store composed with no storage, which keeps its taggregates in memory only.</remarks>
   ///<remarks><b>Its waits are fixed at 30 seconds.</b> It waits at most 30 seconds for the open units of work to complete, then throws,<br/>
   /// having run nothing: units of work that keep overlapping, each beginning before the last has completed, keep it from starting.<br/>
   /// A unit of work that begins during a load again waits at most 30 seconds for it to end, then throws, so a load again that takes<br/>
   /// longer fails the units of work that wait for it. During the store's first load, a unit of work waits as every use of the store<br/>
   /// waits for that load: for at most Compze.Threading's <see cref="LockTimeout.Default"/>.</remarks>
   ///<remarks>In a store kept in tables, a taggregate counts as changed when the state its row holds isn't the state the store held, as the<br/>
   /// store's serializer and the consumer's options write state, so snapshot types need no equality: the row's text is compared with what<br/>
   /// the store would write for the taggregate it held, and where the text differs, the snapshot loaded from it is serialized too. So a<br/>
   /// reload costs one or two serializations for each taggregate the store held. It reads the rows outside any transaction.</remarks>
   ///<remarks>In a store kept in files, a taggregate counts as changed when the state its file holds now isn't the state the store held, as the store's<br/>
   /// serializer and the consumer's options write state, so snapshot types need no equality. A file unchanged since the store last read<br/>
   /// or wrote it, by its length and last write time, holds what the store holds, and is compared with nothing. A changed file is<br/>
   /// compared with what the store would write for the taggregate it held, and where the text differs, the snapshot loaded from it is<br/>
   /// serialized too: the same text is the same state, written otherwise, such as indented differently. So a reload costs nothing for<br/>
   /// an unchanged file, and one or two serializations for a changed one.<br/>
   /// It trusts a file's length and last write time, as a load cache does: a program that rewrites a file with another state of the<br/>
   /// same length and gives it back its last write time isn't seen. And it compares what the serializer writes: where equal states can<br/>
   /// serialize differently, such as a <see cref="HashSet{T}"/> whose order depends on the order its items were added in, a taggregate<br/>
   /// may be reported changed whose state is the same, never the other way round.</remarks>
   ///<remarks>If <paramref name="changeTheStorageFirst"/> throws, nothing is reloaded, and the exception comes out of this call.</remarks>
   ///<returns>Which taggregates the storage held that the store didn't, which it held with another state, and which it no longer holds.</returns>
   ChangesFoundInStorage LoadFromStorage(Action? changeTheStorageFirst = null);

   ///<summary>Writes every taggregate's file anew, as the store's serializer and the consumer's options write its snapshot now, whether<br/>
   /// it changed or not: after a change to the snapshot types or to the serializer's options, so that the files don't stay a mix of the<br/>
   /// old form and the new until each taggregate next changes.</summary>
   ///<remarks>In a store kept in tables: one database transaction, which writes every row. A failure rolls it all back.</remarks>
   ///<remarks>In a store kept in files: one commit, through the journal, as a unit of work's: every file is written under a temporary name, then the journal,<br/>
   /// then the renames. A failure before the journal is written, such as a snapshot that can't be serialized, writes nothing, and comes<br/>
   /// out of this call. A failure after it stops the process (<see cref="Environment.FailFast(string)"/>), as a commit's does: the next<br/>
   /// start finishes it from the journal.<br/>
   /// Waits for the units of work open in the store to complete, and keeps new ones from starting until it is done, as<br/>
   /// <see cref="LoadFromStorage"/> does, with the same 30-second waits.<br/>
   /// Changes no taggregate, nor any version, and dispatches no tevent: a reload afterwards reports nothing changed. Every file has<br/>
   /// then changed, so the next start reads every file, and writes a load cache anew.<br/>
   /// Throws for a store composed with no storage, and inside a unit of work that has changed the store's taggregates.</remarks>
   void WriteAllToStorage();
}

///<summary>How what the store's storage, such as its files, held differed from what the memory image state store held, as <see cref="IMemoryImageStateStore.LoadFromStorage"/> found it.</summary>
public sealed class ChangesFoundInStorage
{
   internal ChangesFoundInStorage(IReadOnlyCollection<TaggregateId> added, IReadOnlyCollection<TaggregateId> changed, IReadOnlyCollection<TaggregateId> removed)
   {
      Added = added;
      Changed = changed;
      Removed = removed;
   }

   ///<summary>The taggregates the storage held that the store didn't, in no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Added { get; }

   ///<summary>The taggregates the storage held with another state than the store did: see <see cref="IMemoryImageStateStore.LoadFromStorage"/>.<br/>
   /// In no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Changed { get; }

   ///<summary>The taggregates the store held that the storage no longer did, in no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Removed { get; }
}
