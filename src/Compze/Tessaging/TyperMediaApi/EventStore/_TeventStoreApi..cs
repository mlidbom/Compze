using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Tessaging.TyperMediaApi.TeventStore;

public partial class TeventStoreApi
{
   public TueryApi Queries => new();
   public TommandApi Tommands => new();

   public partial class TueryApi
   {
      public AggregateLink<TAggregate> GetForUpdate<TAggregate>(Guid id) where TAggregate : class, ITeventStored => new(id);

      public GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy<TAggregate>(Guid id) where TAggregate : class, ITeventStored => new(id);

      public GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion<TAggregate>(Guid id, int version) where TAggregate : class, ITeventStored => new(id, version);

      public GetAggregateHistory<TTevent> GetHistory<TTevent>(Guid id) where TTevent : IAggregateTevent => new(id);
   }

   public partial class TommandApi
   {
      public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) where TAggregate : class, ITeventStored => new(account);
   }
}