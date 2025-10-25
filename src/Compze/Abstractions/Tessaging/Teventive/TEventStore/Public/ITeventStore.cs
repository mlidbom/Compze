using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Public;

public interface ITeventStore : IDisposable
{
   IReadOnlyList<ITaggregateTevent> GetTaggregateHistoryForUpdate(Guid id);
   IReadOnlyList<ITaggregateTevent> GetTaggregateHistory(Guid id);
   void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent> tevents);
   //todo: Utilize C# 8 asynchronous streams.
   void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent>> handleTevents);
   void DeleteTaggregate(Guid taggregateId);
   void PersistMigrations();

   ///<summary>The passed <paramref name="teventType"/> filters the taggregate Ids so that only ids of taggregates that are created by an tevent that inherits from <paramref name="teventType"/> are returned.</summary>
   IEnumerable<Guid> StreamTaggregateIdsInCreationOrder(Type? teventType = null);
}

public static class TeventStoreExtensions
{
   public static IEnumerable<Guid> StreamTaggregateIdsInCreationOrder<TTaggregateTevent>(this ITeventStore @this) => @this.StreamTaggregateIdsInCreationOrder(typeof(TTaggregateTevent));
}

public static class TeventStoreTestingExtensions
{
   public static IReadOnlyList<ITaggregateTevent> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(this ITeventStore @this, int batchSize = 10000)
   {
      var tevents = new List<ITaggregateTevent>();
      @this.StreamTevents(batchSize, tevents.AddRange);
      return tevents;
   }
}