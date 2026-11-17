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
   ///<summary>Every taggregate stored, read once, when the store is first used.</summary>
   IReadOnlyList<TaggregateSnapshot> LoadAll();

   ///<summary>Prepares writing <paramref name="toSave"/>, the new and changed taggregates of a unit of work, and removing<br/>
   /// the taggregates whose ids are <paramref name="toDelete"/>, so that committing the write can't fail.</summary>
   IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete);
}
