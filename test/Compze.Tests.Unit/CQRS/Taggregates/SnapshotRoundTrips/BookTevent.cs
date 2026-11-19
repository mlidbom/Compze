using Compze.Abstractions;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

#pragma warning disable CA1812 // The wrapper tevent classes are built by reflection, around each tevent the book publishes

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

class BookTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IBookTevent<T> where T : IBookTevent;

class BookTevent : TaggregateTevent, IBookTevent
{
   protected BookTevent() {}
   BookTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string title, string author) : BookTevent(taggregateId), IBookTevent.Created
   {
      public string Title { get; } = title;
      public string Author { get; } = author;
   }

   internal class Rated(string reader, int stars) : BookTevent, IBookTevent.Rated
   {
      public string Reader { get; } = reader;
      public int Stars { get; } = stars;
   }

   internal abstract class OfTheChapters : BookTevent, IBookTevent.OfTheChapters
   {
      internal class Added(string chapter) : OfTheChapters, IBookTevent.OfTheChapters.Added { public string Chapter { get; } = chapter; }
   }
}

class BookTagsTevent<T>(T tevent) : BookTevent, IBookTagsTevent<T> where T : ITagSetTevent
{
   public T Tevent { get; } = tevent;
}
