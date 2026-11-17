using Compze.Abstractions;

namespace Compze.Teventive.StateStore.Storage;

///<summary>The state of one taggregate as a state store hands it to its storage and gets it back: a snapshot of the type the<br/>
/// taggregate's class chooses, with what the storage needs to know of the taggregate to keep it, such as where its file is.</summary>
public sealed class TaggregateSnapshot
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

   ///<summary>The taggregate this is a snapshot of, where the one handing it over has it: the state store, when it hands a unit of<br/>
   /// work's changes to be written, and Compze's own storage, when it has restored the taggregate to learn its id. A file's place<br/>
   /// may depend on the taggregate, and a load then restores each taggregate once.</summary>
   internal ITaggregate? Taggregate { get; init; }
}
