using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A taggregate with state in itself, in a component and in a shared tomponent, for the specifications of<br/>
/// <see cref="SnapshotRoundTrip"/>. How it takes and restores its snapshots, mistakes included, is up to the services it's built with.</summary>
class Book : Taggregate<Book, IBookTevent, BookTevent, IBookTevent<IBookTevent>, BookTevent<BookTevent>>, ITeventRouted, ISnapshotable<Book, BookSnapshot>
{
   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<Book, IBookTevent, BookTevent, ITagSetTevent, IBookTagsTevent<ITagSetTevent>> Tags =
         new(typeof(BookTagsTevent<>), static book => book.Tags);
   }

   static readonly TeventRouting<Book, IBookTevent> Appliers = DeclareAppliers(it => it
     .For<IBookTevent.Created>(static (book, tevent) =>
      {
         book.Title = tevent.Title;
         book.Author = tevent.Author;
      })
     .For<IBookTevent.Rated>(static (book, tevent) => book._starsByReader[tevent.Reader] = tevent.Stars)
     .Component(static book => book.Chapters)
     .SharedTomponent(Slots.Tags));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   readonly BookServices _services;
   readonly Dictionary<string, int> _starsByReader = [];
   readonly LazyCE<string> _heading;

   public Book(BookServices services)
   {
      _services = services;
      Chapters = new BookChapters(this);
      Tags = new TagSet(Slots.Tags.In(this));
      _heading = new LazyCE<string>(() => $"{Title}, by {Author}");
   }

   ///<summary>Derived from the book's state on its first read: no state of its own, and no part of the snapshot.</summary>
   public string Heading => _heading.Value;

   public string Title { get; private set; } = "";
   public string? Author { get; private set; }
   public BookChapters Chapters { get; }
   public TagSet Tags { get; }

   public static Book Create(BookServices services, string title, string author)
   {
      var book = new Book(services);
      book.Publish(new BookTevent.Created(new TaggregateId(), title, author));
      return book;
   }

   public void Rate(string reader, int stars) => Publish(new BookTevent.Rated(reader, stars));

   public BookSnapshot TakeSnapshot() =>
      new(Id,
          Title,
          Handling(SnapshotHandling.LeavingOutTheAuthorWhenTakingIt) ? null : Author,
          [..Chapters.All],
          [..Tags.All],
          new Dictionary<string, int>(_starsByReader));

   public void RestoreFrom(BookSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      Title = snapshot.Title;
      Author = snapshot.Author;

      foreach(var (reader, stars) in snapshot.StarsByReader)
         _starsByReader[reader] = Handling(SnapshotHandling.AddingAStarToEachRatingWhenRestoring) ? stars + 1 : stars;

      if(Handling(SnapshotHandling.ReversingTheChaptersWhenRestoring)) Chapters.RestoreFrom(snapshot.Chapters.Reverse());
      else if(!Handling(SnapshotHandling.LeavingOutTheChaptersWhenRestoring)) Chapters.RestoreFrom(snapshot.Chapters);

      if(Handling(SnapshotHandling.AddingTheTagsInReverseWhenRestoring)) Tags.RestoreFrom(snapshot.Tags.Reverse());
      else if(!Handling(SnapshotHandling.LeavingOutTheTagsWhenRestoring)) Tags.RestoreFrom(snapshot.Tags);
   }

   bool Handling(SnapshotHandling handling) => _services.SnapshotHandling == handling;
}
