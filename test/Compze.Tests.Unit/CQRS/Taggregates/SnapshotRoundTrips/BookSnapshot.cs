using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A book's whole state. It doesn't override Equals, which neither a state store nor the round-trip check needs.</summary>
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
}
