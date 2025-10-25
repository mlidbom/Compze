using System;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class TeventStoreApi
{
   public TueryApi Queries => new();
   public TommandApi Tommands => new();

   public partial class TueryApi
   {
      public TaggregateLink<TTaggregate> GetForUpdate<TTaggregate>(Guid id) where TTaggregate : class, ITaggregate => new(id);

      public GetReadonlyCopyOfTaggregate<TTaggregate> GetReadOnlyCopy<TTaggregate>(Guid id) where TTaggregate : class, ITaggregate => new(id);

      public GetReadonlyCopyOfTaggregateVersion<TTaggregate> GetReadOnlyCopyOfVersion<TTaggregate>(Guid id, int version) where TTaggregate : class, ITaggregate => new(id, version);

      public GetTaggregateHistory<TTevent> GetHistory<TTevent>(Guid id) where TTevent : ITaggregateTevent => new(id);
   }

   public partial class TommandApi
   {
      public SaveTaggregate<TTaggregate> Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate => new(taggregate);
   }
}