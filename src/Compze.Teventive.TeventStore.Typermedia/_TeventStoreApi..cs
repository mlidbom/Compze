using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Teventive.TeventStore.Typermedia;

public partial class TeventStoreApi
{
   public TueryApi Tueries => new();
   public TommandApi Tommands => new();

   public partial class TueryApi
   {
      public TaggregateLink<TTaggregate> GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> =>
         new(id);

      public GetTaggregate<TTaggregate> Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> =>
         new(id);

      public GetReadonlyCopyOfTaggregateVersion<TTaggregate> GetReadOnlyCopyOfVersion<TTaggregate>(TaggregateId id, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> =>
         new(id, version);

      public GetTaggregateHistory<TTevent> GetHistory<TTevent>(TaggregateId id) where TTevent : class, ITaggregateTeventWithIdentity =>
         new(id);
   }

   public partial class TommandApi
   {
      public AddTaggregate<TTaggregate> Add<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity> =>
         new(taggregate);
   }
}
