using System.Diagnostics;
using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>The storage of a memory image state store that keeps its taggregates in memory only: it loads nothing, and a write does nothing.</summary>
sealed class NoStorage : ITaggregateStorage, IPreparedWrite
{
   public LoadedTaggregates LoadAll() => new([], holdingEveryCommitThrough: null);
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete) => this;

   ///<summary>Never asked: the store refuses a reload when it has no storage.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot) => throw new UnreachableException("A memory image state store with no storage is never reloaded.");

   ///<summary>Never asked: the store refuses to write everything anew when it has no storage.</summary>
   public void WriteAll(IReadOnlyList<TaggregateSnapshot> everything) => throw new UnreachableException("A memory image state store with no storage never writes everything anew.");

   ///<summary>No other memory image shares it, so there is no lock to take, and nothing newer to find.</summary>
   public StoredTaggregate? LockForUpdate(TaggregateId id, CommitNumber? heldAs) => null;

   public TaggregatesCommittedAfter CommittedAfter(CommitNumber lastSeen) => TaggregatesCommittedAfter.None(lastSeen);

   public void Commit() {}
   public void Discard() {}
   public CommitNumber? CommittedAs => null;
}
