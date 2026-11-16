using Compze.Teventive.StateStore.Storage;

namespace Compze.Teventive.StateStore._private;

///<summary>The storage of a state store that keeps its taggregates in memory only: it loads nothing, and a write does nothing.</summary>
sealed class NoStorage : ITaggregateStorage, IPreparedWrite
{
   public IReadOnlyList<TaggregateSnapshot> LoadAll() => [];
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateToDelete> toDelete) => this;
   public void Commit() {}
   public void Discard() {}
}
