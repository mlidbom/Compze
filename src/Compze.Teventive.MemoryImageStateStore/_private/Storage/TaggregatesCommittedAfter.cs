namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>What a storage that several memory images share holds of the commits after a number a memory image had seen: each<br/>
/// taggregate they wrote, and the number through which this holds every commit.</summary>
sealed class TaggregatesCommittedAfter
{
   internal TaggregatesCommittedAfter(IReadOnlyList<StoredTaggregate> taggregates, CommitNumber holdingEveryCommitThrough)
   {
      Taggregates = taggregates;
      HoldingEveryCommitThrough = holdingEveryCommitThrough;
   }

   ///<summary>Nothing: what a storage that no other memory image shares, such as files, holds of the commits after <paramref name="lastSeen"/>.</summary>
   internal static TaggregatesCommittedAfter None(CommitNumber lastSeen) => new([], lastSeen);

   ///<summary>Each taggregate the commits wrote, as the last of them left it. Some may be of commits after<br/>
   /// <see cref="HoldingEveryCommitThrough"/>, which the next read gives again.</summary>
   internal IReadOnlyList<StoredTaggregate> Taggregates { get; }

   ///<summary>The number through which <see cref="Taggregates"/> holds every commit: the next read reads the commits after it.</summary>
   internal CommitNumber HoldingEveryCommitThrough { get; }
}
