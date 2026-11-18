using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;

namespace Compze.Teventive.StateStore;

///<summary>What a state store does as a whole, beyond reading and changing its taggregates through <see cref="ITaggregateStoreReader"/>,<br/>
/// <see cref="ITaggregateStoreBulkReader"/> and <see cref="ITaggregateStoreUpdater"/>. Resolved from the container that composes the store.</summary>
public interface IStateStore
{
   ///<summary>Loads every taggregate from the storage again, as when a program other than this one has changed what the storage holds,<br/>
   /// and replaces the committed taggregates with what it loaded. Dispatches no tevent.<br/>
   /// With <paramref name="changeToTheStorage"/>, runs it first, once the open units of work have completed, while new ones wait:<br/>
   /// such as a fast-forward of the checkout that holds the consumer's files, which no unit of work may write a taggregate over.</summary>
   ///<remarks>Waits for the units of work open in the store to complete, and keeps new ones from starting until it is done. Every<br/>
   /// <see cref="ITaggregateReference{TTaggregate}"/> is kept, and gives the reloaded state; the indexes file the reloaded state.<br/>
   /// Throws for a store composed with no storage, which keeps its taggregates in memory only.</remarks>
   ///<remarks>A taggregate counts as changed when the state its file holds now isn't the state the store held, as the store's<br/>
   /// serializer and the consumer's options write state, so snapshot types need no equality. A file unchanged since the store last read<br/>
   /// or wrote it, by its length and last write time, holds what the store holds, and is compared with nothing. A changed file is<br/>
   /// compared with what the store would write for the taggregate it held, and where the text differs, the snapshot loaded from it is<br/>
   /// serialized too: the same text is the same state, written otherwise, such as indented differently. So a reload costs nothing for<br/>
   /// an unchanged file, and one or two serializations for a changed one.<br/>
   /// It trusts a file's length and last write time, as a load cache does: a program that rewrites a file with another state of the<br/>
   /// same length and gives it back its last write time isn't seen. And it compares what the serializer writes: where equal states can<br/>
   /// serialize differently, such as a <see cref="HashSet{T}"/> whose order depends on the order its items were added in, a taggregate<br/>
   /// may be reported changed whose state is the same, never the other way round.</remarks>
   ///<remarks>If <paramref name="changeToTheStorage"/> throws, nothing is reloaded, and the exception comes out of this call.</remarks>
   ///<returns>Which taggregates the storage held that the store didn't, which it held with another state, and which it no longer holds.</returns>
   ReloadedTaggregates ReloadFromStorage(Action? changeToTheStorage = null);

   ///<summary>Writes every taggregate's file anew, as the store's serializer and the consumer's options write its snapshot now, whether<br/>
   /// it changed or not: after a change to the snapshot types or to the serializer's options, so that the files don't stay a mix of the<br/>
   /// old form and the new until each taggregate next changes.</summary>
   ///<remarks>One commit, through the journal, as a unit of work's: every file is written under a temporary name, then the journal,<br/>
   /// then the renames. A failure before the journal is written, such as a snapshot that can't be serialized, writes nothing, and comes<br/>
   /// out of this call. A failure after it stops the process (<see cref="Environment.FailFast(string)"/>), as a commit's does: the next<br/>
   /// start finishes it from the journal.<br/>
   /// Waits for the units of work open in the store to complete, and keeps new ones from starting until it is done, as a reload does.<br/>
   /// Changes no taggregate, nor any version, and dispatches no tevent: a reload afterwards reports nothing changed. Every file has<br/>
   /// then changed, so the next start reads every file, and writes a load cache anew.<br/>
   /// Throws for a store composed with no storage, and inside a unit of work that has changed the store's taggregates.<br/>
   /// review-naming: WriteEverythingAnew, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-18).</remarks>
   void WriteEverythingAnew();
}

///<summary>How what the store's storage, such as its files, held differed from what the state store held, as <see cref="IStateStore.ReloadFromStorage"/> found it.</summary>
public sealed class ReloadedTaggregates
{
   internal ReloadedTaggregates(IReadOnlyCollection<TaggregateId> added, IReadOnlyCollection<TaggregateId> changed, IReadOnlyCollection<TaggregateId> removed)
   {
      Added = added;
      Changed = changed;
      Removed = removed;
   }

   ///<summary>The taggregates the storage held that the store didn't, in no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Added { get; }

   ///<summary>The taggregates the storage held with another state than the store did: see <see cref="IStateStore.ReloadFromStorage"/>.<br/>
   /// In no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Changed { get; }

   ///<summary>The taggregates the store held that the storage no longer did, in no particular order.</summary>
   public IReadOnlyCollection<TaggregateId> Removed { get; }
}
