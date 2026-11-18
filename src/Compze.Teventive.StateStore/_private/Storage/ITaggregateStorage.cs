using Compze.Abstractions;

namespace Compze.Teventive.StateStore._private.Storage;

///<summary>Where a state store keeps what it has committed, such as a file for each taggregate. Narrow: it knows snapshots of the<br/>
/// taggregates' state, which it may write in any form it likes, not taggregates.</summary>
///<remarks>A write has two steps, because a transaction has two. The store calls <see cref="Prepare"/> when the unit of work's<br/>
/// transaction prepares, once its tevents are dispatched, and <see cref="IPreparedWrite.Commit"/> or<br/>
/// <see cref="IPreparedWrite.Discard"/> when it commits or rolls back. Everything that can fail belongs in <see cref="Prepare"/>:<br/>
/// serializing, writing, a full disk, a file another program holds.</remarks>
interface ITaggregateStorage
{
   ///<summary>Every taggregate stored, read when the store is first used, and at each reload.</summary>
   IReadOnlyList<TaggregateSnapshot> LoadAll();

   ///<summary>Whether <paramref name="loaded"/>, a snapshot <see cref="LoadAll"/> has just given of a taggregate the store held,<br/>
   /// holds the state that <paramref name="heldSnapshot"/>, a snapshot of the instance the store held, holds: as the storage writes<br/>
   /// state, so that snapshot types need no equality. Asked by a reload, of a snapshot whose class is the held instance's, and that<br/>
   /// isn't <see cref="TaggregateSnapshot.IsUnchangedSinceTheStoreLastReadOrWroteIt"/>.</summary>
   bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot);

   ///<summary>Prepares writing <paramref name="toSave"/>, the new and changed taggregates of a unit of work, and removing<br/>
   /// the taggregates whose ids are <paramref name="toDelete"/>, so that committing the write can't fail.</summary>
   IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete);

   ///<summary>Writes <paramref name="everything"/>, a snapshot of every taggregate the store holds, anew, as one commit, outside any<br/>
   /// transaction: <see cref="IStateStore.WriteAllToStorage"/>.</summary>
   void WriteAll(IReadOnlyList<TaggregateSnapshot> everything);
}
