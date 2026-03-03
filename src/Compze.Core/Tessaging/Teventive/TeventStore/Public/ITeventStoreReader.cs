using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public;

public interface ITeventStoreReader
{
   IReadOnlyList<ITaggregateTevent> GetHistory(TaggregateId taggregateId);
   /// <summary>
   /// Loads a specific version of the taggregate.
   /// This instance is NOT tracked for changes.
   /// No changes to this entity vill be persisted.
   /// </summary>
   TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate;

   TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate;
}
