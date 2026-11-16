using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A book's whole state. It doesn't override Equals: the comparison of two is given with the class, as for a snapshot type<br/>
/// whose own equality wouldn't compare its collections by what they hold.</summary>
class BookSnapshot
{
   public BookSnapshot(TaggregateId id, string title, string? author, IReadOnlyList<string> chapters, IReadOnlyList<string> tags, IReadOnlyDictionary<string, int> starsByReader)
   {
      Id = id;
      Title = title;
      Author = author;
      Chapters = chapters;
      Tags = tags;
      StarsByReader = starsByReader;
   }

   public TaggregateId Id { get; }
   public string Title { get; }
   public string? Author { get; }
   public IReadOnlyList<string> Chapters { get; }
   public IReadOnlyList<string> Tags { get; }
   public IReadOnlyDictionary<string, int> StarsByReader { get; }

   ///<summary>Whether two snapshots hold the same state: the tags in any order, since a set of tags has none.</summary>
   public static bool HoldTheSameState(BookSnapshot one, BookSnapshot other) =>
      HoldTheSameStateApartFromTheTags(one, other) && one.Tags.ToHashSet().SetEquals(other.Tags);

   ///<summary>A mistaken comparison of two snapshots, which compares their tags in order.</summary>
   public static bool HoldTheSameStateComparingTheTagsInOrder(BookSnapshot one, BookSnapshot other) =>
      HoldTheSameStateApartFromTheTags(one, other) && one.Tags.SequenceEqual(other.Tags);

   static bool HoldTheSameStateApartFromTheTags(BookSnapshot one, BookSnapshot other) =>
      one.Id == other.Id
   && one.Title == other.Title
   && one.Author == other.Author
   && one.Chapters.SequenceEqual(other.Chapters)
   && one.StarsByReader.Count == other.StarsByReader.Count
   && one.StarsByReader.All(it => other.StarsByReader.TryGetValue(it.Key, out var stars) && stars == it.Value);
}
