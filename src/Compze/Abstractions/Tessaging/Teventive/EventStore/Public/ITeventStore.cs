using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;

public interface ITeventStore : IDisposable
{
   IReadOnlyList<IAggregateTevent> GetAggregateHistoryForUpdate(Guid id);
   IReadOnlyList<IAggregateTevent> GetAggregateHistory(Guid id);
   void SaveSingleAggregateTevents(IReadOnlyList<IAggregateTevent> tevents);
   //todo: Utilize C# 8 asynchronous streams.
   void StreamTevents(int batchSize, Action<IReadOnlyList<IAggregateTevent>> handleTevents);
   void DeleteAggregate(Guid aggregateId);
   void PersistMigrations();

   ///<summary>The passed <paramref name="teventType"/> filters the aggregate Ids so that only ids of aggregates that are created by an tevent that inherits from <paramref name="teventType"/> are returned.</summary>
   IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? teventType = null);
}

public static class TeventStoreExtensions
{
   public static IEnumerable<Guid> StreamAggregateIdsInCreationOrder<TAggregateTevent>(this ITeventStore @this) => @this.StreamAggregateIdsInCreationOrder(typeof(TAggregateTevent));
}

public static class TeventStoreTestingExtensions
{
   public static IReadOnlyList<IAggregateTevent> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(this ITeventStore @this, int batchSize = 10000)
   {
      var tevents = new List<IAggregateTevent>();
      @this.StreamTevents(batchSize, tevents.AddRange);
      return tevents;
   }
}