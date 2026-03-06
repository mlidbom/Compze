using Compze.Abstractions.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Tessaging.TyperMediaApi.EventStore;

public partial class TeventStoreApi
{
   public TueryApi Tueries => new();
   public TommandApi Tommands => new();

   public partial class TueryApi
   {
      public TaggregateLink<TTaggregate> GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
         new(id);

      public GetReadonlyCopyOfTaggregate<TTaggregate> GetReadOnlyCopy<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
         new(id);

      public GetReadonlyCopyOfTaggregateVersion<TTaggregate> GetReadOnlyCopyOfVersion<TTaggregate>(TaggregateId id, int version) where TTaggregate : class, ITaggregate =>
         new(id, version);

      //Todo: should be aggregateId
      public GetTaggregateHistory<TTevent> GetHistory<TTevent>(TaggregateId id) where TTevent : ITaggregateTevent =>
         new(id);
   }

   public partial class TommandApi
   {
      public SaveTaggregate<TTaggregate> Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate =>
         new(taggregate);
   }
}
