using System.Diagnostics;
using Compze.Abstractions;

namespace Compze.Teventive.StateStore._private.Storage;

///<summary>The storage of a state store that keeps its taggregates in memory only: it loads nothing, and a write does nothing.</summary>
sealed class NoStorage : ITaggregateStorage, IPreparedWrite
{
   public IReadOnlyList<TaggregateSnapshot> LoadAll() => [];
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete) => this;

   ///<summary>Never asked: the store refuses a reload when it has no storage.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot) => throw new UnreachableException("A state store with no storage is never reloaded.");

   ///<summary>Never asked: the store refuses to write everything anew when it has no storage.</summary>
   public void WriteAll(IReadOnlyList<TaggregateSnapshot> everything) => throw new UnreachableException("A state store with no storage never writes everything anew.");
   public void Commit() {}
   public void Discard() {}
}
