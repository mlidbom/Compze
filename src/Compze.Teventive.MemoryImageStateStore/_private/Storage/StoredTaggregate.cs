using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>One taggregate as a storage that several memory images share holds it, found committed by another memory image than the<br/>
/// one that reads it: its state, or that it was deleted, and the number of the commit that wrote it.</summary>
sealed class StoredTaggregate
{
   StoredTaggregate(TaggregateId id, CommitNumber committedAs, TaggregateSnapshot? snapshot)
   {
      Id = id;
      CommittedAs = committedAs;
      Snapshot = snapshot;
   }

   ///<summary>The taggregate stored with the state <paramref name="snapshot"/> by the commit <paramref name="committedAs"/>.</summary>
   internal static StoredTaggregate WithState(TaggregateSnapshot snapshot, CommitNumber committedAs) => new(snapshot.Id, committedAs, snapshot);

   ///<summary>The taggregate <paramref name="id"/>, deleted by the commit <paramref name="committedAs"/>.</summary>
   internal static StoredTaggregate Deleted(TaggregateId id, CommitNumber committedAs) => new(id, committedAs, snapshot: null);

   internal TaggregateId Id { get; }

   ///<summary>The number of the commit that last wrote the taggregate: that changed it, added it, or deleted it.</summary>
   internal CommitNumber CommittedAs { get; }

   ///<summary>The taggregate's state, or null when <see cref="IsDeleted"/>.</summary>
   internal TaggregateSnapshot? Snapshot { get; }

   internal bool IsDeleted => Snapshot == null;
}
