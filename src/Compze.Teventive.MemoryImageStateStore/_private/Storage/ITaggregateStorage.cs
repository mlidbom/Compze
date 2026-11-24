using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>Where a memory image state store keeps what it has committed, such as a file for each taggregate. Narrow: it knows snapshots of the<br/>
/// taggregates' state, which it may write in any form it likes, not taggregates.</summary>
///<remarks>A write has two steps, because a transaction has two. The store calls <see cref="Prepare"/> when the unit of work's<br/>
/// transaction prepares, once its tevents are dispatched, and <see cref="IPreparedWrite.Commit"/> or<br/>
/// <see cref="IPreparedWrite.Discard"/> when it commits or rolls back. Everything that can fail belongs in <see cref="Prepare"/>:<br/>
/// serializing, writing, a full disk, a file another program holds.</remarks>
///<remarks>A storage that several memory images share, such as a database's tables, numbers its commits (<see cref="CommitNumber"/>):<br/>
/// <see cref="LockForUpdate"/> and <see cref="CommittedAfter"/> give what the other memory images committed. A storage that no<br/>
/// other memory image shares, such as files, gives nothing from them.</remarks>
interface ITaggregateStorage
{
   ///<summary>Every taggregate stored, read when the store is first used, and at each reload.</summary>
   LoadedTaggregates LoadAll();

   ///<summary>Whether <paramref name="loaded"/>, a snapshot <see cref="LoadAll"/> has just given of a taggregate the store held,<br/>
   /// holds the state that <paramref name="heldSnapshot"/>, a snapshot of the instance the store held, holds: as the storage writes<br/>
   /// state, so that snapshot types need no equality. Asked by a reload, of a snapshot whose class is the held instance's, which has<br/>
   /// no <see cref="TaggregateSnapshot.CommittedAs"/>, and that isn't <see cref="TaggregateSnapshot.IsUnchangedSinceTheStoreLastReadOrWroteIt"/>.</summary>
   bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot);

   ///<summary>Called inside a unit of work's transaction, for each taggregate it gets for update or adds, once it holds the<br/>
   /// taggregate's lock in memory: takes the taggregate's lock in the storage, held until the transaction completes, which the<br/>
   /// other memory images sharing the storage respect. Gives the taggregate as stored when a commit after <paramref name="heldAs"/>,<br/>
   /// the commit the memory image holds it as, if any, has written it; else null.</summary>
   StoredTaggregate? LockForUpdate(TaggregateId id, CommitNumber? heldAs);

   ///<summary>What the commits after <paramref name="lastSeen"/> wrote: the other memory images' commits, and this one's own, which<br/>
   /// the store knows it holds by their numbers. Read outside any transaction.</summary>
   TaggregatesCommittedAfter CommittedAfter(CommitNumber lastSeen);

   ///<summary>Prepares writing <paramref name="toSave"/>, the new and changed taggregates of a unit of work, and removing<br/>
   /// the taggregates whose ids are <paramref name="toDelete"/>, so that committing the write can't fail.</summary>
   IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete);

   ///<summary>Writes every taggregate the store holds anew, as one commit, outside any unit of work:<br/>
   /// <see cref="IMemoryImageStateStore.WriteAllToStorage"/>. <paramref name="everythingOnceCaughtUpWith"/> is handed what the other<br/>
   /// memory images committed after <paramref name="lastSeen"/>, at a moment when none can commit until this write has, and gives a<br/>
   /// snapshot of every taggregate the store then holds.</summary>
   ///<returns>The number the write committed as, for a storage that numbers its commits; else null.</returns>
   CommitNumber? WriteAll(CommitNumber lastSeen, Func<IReadOnlyList<StoredTaggregate>, IReadOnlyList<TaggregateSnapshot>> everythingOnceCaughtUpWith);
}
