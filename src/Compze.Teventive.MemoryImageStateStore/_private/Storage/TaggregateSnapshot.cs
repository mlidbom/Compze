using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>The state of one taggregate as a memory image state store hands it to its storage and gets it back: a snapshot of the type the<br/>
/// taggregate's class chooses, with what the storage knows of it when it loads it.</summary>
sealed class TaggregateSnapshot
{
   ///<summary>The snapshot of a taggregate of <paramref name="taggregateClass"/> with the id <paramref name="id"/>.</summary>
   public TaggregateSnapshot(Type taggregateClass, TaggregateId id, object snapshot)
   {
      TaggregateClass = taggregateClass;
      Id = id;
      Snapshot = snapshot;
   }

   ///<summary>The taggregate's class, one of those the store is declared to keep.</summary>
   public Type TaggregateClass { get; }

   ///<summary>The taggregate's id.</summary>
   public TaggregateId Id { get; }

   ///<summary>The snapshot, of the type the taggregate's class takes and restores from.</summary>
   public object Snapshot { get; }

   ///<summary>The taggregate restored from this snapshot, where Compze's own storage restored it to learn its id, so that a load<br/>
   /// restores each taggregate once.</summary>
   internal ITaggregate? RestoredTaggregate { get; init; }

   ///<summary>Whether the storage knows, as it loads this snapshot, that it holds the state it held when the store last read or wrote<br/>
   /// the taggregate: such as a file that no other program has changed since. A reload compares such a snapshot with nothing.</summary>
   internal bool IsUnchangedSinceTheStoreLastReadOrWroteIt { get; init; }

   ///<summary>The text the storage read this snapshot from, as it loads it, where it may hold another state than the store held, such as<br/>
   /// a changed file: what a reload compares first, with what the storage would write for the taggregate the store held.</summary>
   internal string? TextReadFrom { get; init; }
}
