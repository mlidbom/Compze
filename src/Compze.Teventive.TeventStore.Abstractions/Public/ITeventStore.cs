using Compze.Abstractions.Public;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Public;

///<summary>Persists and loads taggregate histories. The store's currency is the wrapped tevent - every tevent exactly as its taggregate published it,<br/>
/// inside its publisher's <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper - persisted under the wrapper type's identity with zero information loss.</summary>
public interface ITeventStore : IDisposable
{
   IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id);
   IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id);
   void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents);
   //todo: Utilize C# 8 asynchronous streams.
   void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents);
   void DeleteTaggregate(TaggregateId taggregateId);
   void PersistMigrations();

   ///<summary>The passed <paramref name="teventType"/> filters the taggregate Ids so that only ids of taggregates that are created by a tevent compatible with <paramref name="teventType"/> are returned.<br/>
   /// An inner tevent type matches every wrapping of it; a wrapper type matches as it stands.</summary>
   IEnumerable<TaggregateId> StreamTaggregateIdsInCreationOrder(Type? teventType = null);
}

public static class TeventStoreExtensions
{
   public static IEnumerable<TaggregateId> StreamTaggregateIdsInCreationOrder<TTaggregateTevent>(this ITeventStore @this) => @this.StreamTaggregateIdsInCreationOrder(typeof(TTaggregateTevent));
}

public static class TeventStoreTestingExtensions
{
   public static IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(this ITeventStore @this, int batchSize = 10000)
   {
      var wrappedTevents = new List<ITaggregateTevent<ITaggregateTevent>>();
      @this.StreamTevents(batchSize, wrappedTevents.AddRange);
      return wrappedTevents;
   }
}