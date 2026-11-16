using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

interface IBookTevent : ITaggregateTevent
{
   interface Created : IBookTevent, ITaggregateCreatedTevent
   {
      string Title { get; }
      string Author { get; }
   }

   interface Rated : IBookTevent
   {
      string Reader { get; }
      int Stars { get; }
   }

   ///<summary>The tevents of the book's chapters, a component of the book.</summary>
   interface OfTheChapters : IBookTevent
   {
      interface Added : OfTheChapters { string Chapter { get; } }
   }
}

interface IBookTevent<out T> : ITaggregateTevent<T> where T : IBookTevent;

///<summary>The adopting wrapper tevent of the book's slot for its tags, a shared tomponent.</summary>
interface IBookTagsTevent<out T> : IBookTevent, IPublisherTevent<T> where T : ITagSetTevent;
