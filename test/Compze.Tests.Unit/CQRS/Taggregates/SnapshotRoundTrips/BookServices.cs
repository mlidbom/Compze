namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>What every book is built with, as an application's taggregates may be built with its services: a copy of a book is built<br/>
/// with the same instance. For the specifications, it says how the books handle their snapshots, mistakes included.</summary>
class BookServices(SnapshotHandling snapshotHandling)
{
   public SnapshotHandling SnapshotHandling { get; } = snapshotHandling;
}

///<summary>How a book takes and restores its snapshots: correctly, or with one of the mistakes a snapshot round trip must catch.</summary>
enum SnapshotHandling
{
   Correctly,
   AddingTheTagsInReverseWhenRestoring,
   LeavingOutTheAuthorWhenTakingIt,
   LeavingOutTheChaptersWhenRestoring,
   ReversingTheChaptersWhenRestoring,
   LeavingOutTheTagsWhenRestoring,
   AddingAStarToEachRatingWhenRestoring
}
