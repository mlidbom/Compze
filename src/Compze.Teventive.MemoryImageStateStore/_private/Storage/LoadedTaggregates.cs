namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>Every taggregate a storage holds, as <see cref="ITaggregateStorage.LoadAll"/> loads it.</summary>
sealed class LoadedTaggregates
{
   internal LoadedTaggregates(IReadOnlyList<TaggregateSnapshot> snapshots, CommitNumber? holdingEveryCommitThrough)
   {
      Snapshots = snapshots;
      HoldingEveryCommitThrough = holdingEveryCommitThrough;
   }

   internal IReadOnlyList<TaggregateSnapshot> Snapshots { get; }

   ///<summary>For a storage that numbers its commits, the number through which <see cref="Snapshots"/> holds every commit, so that<br/>
   /// catching up reads the commits after it. Null for a storage that doesn't, such as files.</summary>
   internal CommitNumber? HoldingEveryCommitThrough { get; }
}
