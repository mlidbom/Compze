using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A book's chapters: a component of the book, which holds a reference back to it.</summary>
class BookChapters : TeventiveComponent<Book, IBookTevent, BookTevent, BookChapters, IBookTevent.OfTheChapters, BookTevent.OfTheChapters>
{
   static readonly TeventRouting<BookChapters, IBookTevent.OfTheChapters> Appliers = DeclareAppliers(it => it
     .For<IBookTevent.OfTheChapters.Added>(static (chapters, tevent) => chapters._chapters.Add(tevent.Chapter)));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly List<string> _chapters = [];

   public BookChapters(Book book) : base(book) => Book = book;

   ///<summary>The book these are the chapters of: a reference back of the component's own, which a comparison of states must not<br/>
   /// follow round forever.</summary>
   public Book Book { get; }

   public IReadOnlyList<string> All => _chapters;

   public void Add(string chapter) => Publish(new BookTevent.OfTheChapters.Added(chapter));

   internal void RestoreFrom(IEnumerable<string> chapters) => _chapters.AddRange(chapters);
}
